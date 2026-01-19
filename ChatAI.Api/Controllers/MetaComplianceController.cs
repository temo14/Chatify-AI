using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Meta App Review compliance callbacks.
/// Configure these URLs in the Meta App dashboard:
/// - Deauthorize Callback URL
/// - Data Deletion Callback URL
/// </summary>
[ApiController]
[Route("api/meta/compliance")]
[AllowAnonymous]
public class MetaComplianceController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MetaComplianceController> _logger;

    public MetaComplianceController(
        IConfiguration configuration,
        IDistributedCache cache,
        ILogger<MetaComplianceController> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("deauthorize")]
    public async Task<IActionResult> Deauthorize(CancellationToken cancellationToken)
    {
        var signedRequest = await GetSignedRequestAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(signedRequest))
        {
            return BadRequest(new { error = "missing_signed_request" });
        }

        if (!TryValidateAndParseSignedRequest(signedRequest, out var payload, out var error))
        {
            _logger.LogWarning("Invalid signed_request for deauthorize: {Error}", error);
            return Unauthorized();
        }

        // We do not persist Meta user data tied to Facebook Login user_id in this platform flow.
        // Still, we ACK and log for auditability.
        payload.TryGetProperty("user_id", out var userIdEl);
        var userId = userIdEl.ValueKind == JsonValueKind.String ? userIdEl.GetString() : null;

        _logger.LogInformation("Meta deauthorize callback received. user_id={UserId}", userId);

        // If you later store data tied to Meta user_id, purge it here.
        return Ok(new { success = true });
    }

    [HttpPost("data-deletion")]
    public async Task<IActionResult> DataDeletion(CancellationToken cancellationToken)
    {
        var signedRequest = await GetSignedRequestAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(signedRequest))
        {
            return BadRequest(new { error = "missing_signed_request" });
        }

        if (!TryValidateAndParseSignedRequest(signedRequest, out var payload, out var error))
        {
            _logger.LogWarning("Invalid signed_request for data deletion: {Error}", error);
            return Unauthorized();
        }

        payload.TryGetProperty("user_id", out var userIdEl);
        var userId = userIdEl.ValueKind == JsonValueKind.String ? userIdEl.GetString() : null;

        // Generate a confirmation code and store it temporarily.
        var confirmationCode = Guid.NewGuid().ToString("N");
        await _cache.SetStringAsync(
            $"meta_data_deletion:{confirmationCode}",
            JsonSerializer.Serialize(new { user_id = userId, requested_at = DateTime.UtcNow }),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) },
            cancellationToken);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var statusUrl = $"{baseUrl}/api/meta/compliance/data-deletion/status?code={confirmationCode}";

        _logger.LogInformation("Meta data deletion callback received. user_id={UserId} confirmation_code={Code}", userId, confirmationCode);

        return Ok(new
        {
            url = statusUrl,
            confirmation_code = confirmationCode
        });
    }

    [HttpGet("data-deletion/status")]
    public async Task<IActionResult> DataDeletionStatus([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Missing code");
        }

        var key = $"meta_data_deletion:{code}";
        var value = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return NotFound("Unknown or expired confirmation code");
        }

        return Ok(new { status = "received", confirmation_code = code });
    }

    private async Task<string?> GetSignedRequestAsync(CancellationToken cancellationToken)
    {
        // Meta typically posts as form-urlencoded with field: signed_request
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            return form["signed_request"].FirstOrDefault();
        }

        // Fallback: allow JSON body { "signed_request": "..." }
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("signed_request", out var sr) && sr.ValueKind == JsonValueKind.String)
            {
                return sr.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private bool TryValidateAndParseSignedRequest(string signedRequest, out JsonElement payload, out string error)
    {
        payload = default;
        error = string.Empty;

        var parts = signedRequest.Split('.', 2);
        if (parts.Length != 2)
        {
            error = "invalid_format";
            return false;
        }

        var signature = Base64UrlDecode(parts[0]);
        var payloadBytes = Base64UrlDecode(parts[1]);

        var appSecret = _configuration["Meta:AppSecret"]
            ?? _configuration["Meta:OAuth:ClientSecret"]
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(appSecret))
        {
            error = "missing_app_secret";
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var expected = hmac.ComputeHash(payloadBytes);

        if (expected.Length != signature.Length || !CryptographicOperations.FixedTimeEquals(expected, signature))
        {
            error = "invalid_signature";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadBytes);
            payload = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            error = "invalid_payload_json";
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
