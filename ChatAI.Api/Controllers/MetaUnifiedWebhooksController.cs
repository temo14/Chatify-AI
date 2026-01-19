using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Infrastructure.Services.Meta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Unified Meta webhooks endpoint (AllowAnonymous - validated via signature)
/// Target URL: /api/webhooks/meta
/// </summary>
[ApiController]
[Route("api/webhooks/meta")]
[AllowAnonymous]
public class MetaUnifiedWebhooksController : ControllerBase
{
    private readonly IMetaChannelConnectionRepository _connectionRepository;
    private readonly IMetaWebhookSignatureValidator _signatureValidator;
    private readonly IMetaWebhookQueue _webhookQueue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaUnifiedWebhooksController> _logger;

    public MetaUnifiedWebhooksController(
        IMetaChannelConnectionRepository connectionRepository,
        IMetaWebhookSignatureValidator signatureValidator,
        IMetaWebhookQueue webhookQueue,
        IConfiguration configuration,
        ILogger<MetaUnifiedWebhooksController> logger)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _webhookQueue = webhookQueue ?? throw new ArgumentNullException(nameof(webhookQueue));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode != "subscribe" || string.IsNullOrEmpty(verifyToken) || string.IsNullOrEmpty(challenge))
        {
            return StatusCode(403);
        }

        var expected = _configuration["Meta:Webhook:VerifyToken"];
        if (string.IsNullOrEmpty(expected) || !string.Equals(verifyToken, expected, StringComparison.Ordinal))
        {
            return StatusCode(403);
        }

        return Content(challenge, "text/plain");
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📨 Meta webhook received");
            
            Request.EnableBuffering();

            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync(cancellationToken);
                Request.Body.Position = 0;
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                _logger.LogWarning("Empty webhook body received");
                return Ok();
            }

            _logger.LogInformation("📨 Webhook body length: {Length} bytes", rawBody.Length);

            var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                _logger.LogError("❌ Missing X-Hub-Signature-256 header");
                return Unauthorized();
            }

            _logger.LogInformation("🔐 Signature header present: {Signature}", signatureHeader.Substring(0, Math.Min(20, signatureHeader.Length)) + "...");

            // Shared Meta App secret (single Meta App for the whole platform)
            var appSecret = _configuration["Meta:AppSecret"]
                ?? _configuration["Meta:OAuth:ClientSecret"]
                ?? throw new InvalidOperationException("Meta AppSecret not configured");

            _logger.LogInformation("🔑 App secret loaded, length: {Length} chars, first 4: {Preview}...", 
                appSecret.Length, 
                appSecret.Substring(0, Math.Min(4, appSecret.Length)));

            if (!_signatureValidator.ValidateSignature(rawBody, signatureHeader, appSecret))
            {
                _logger.LogError("❌ Signature validation FAILED - Expected signature doesn't match X-Hub-Signature-256 header");
                _logger.LogError("   Body length: {Length}, Signature: {Sig}", rawBody.Length, signatureHeader);
                return Unauthorized();
            }

            _logger.LogInformation("✅ Signature validation passed");

            if (!TryResolveRouting(rawBody, out var channel, out var identityValue))
            {
                _logger.LogWarning("Could not resolve webhook routing from payload (signature ok). Body preview: {Preview}",
                    rawBody.Substring(0, Math.Min(300, rawBody.Length)));
                return Ok();
            }

            _logger.LogInformation("📍 Routing resolved: Channel={Channel}, Identity={Identity}", channel, identityValue);

            var connection = await _connectionRepository.FindByChannelIdentityAsync(channel, identityValue, cancellationToken);
            if (connection == null)
            {
                _logger.LogWarning("❌ No active connection found for {Channel} identity {Identity}", channel, identityValue);
                return Ok();
            }

            _logger.LogInformation("✅ Connection found: {ConnectionId}", connection.Id);

            var externalUserId = TryExtractExternalUserId(rawBody, channel);

            var eventKey = Guid.NewGuid().ToString();
            await _webhookQueue.EnqueueAsync(new MetaWebhookMessage
            {
                ConnectionId = connection.Id,
                Channel = channel,
                ExternalUserId = externalUserId,
                RawPayload = rawBody,
                ReceivedAt = DateTime.UtcNow,
                EventKey = eventKey
            }, cancellationToken);

            _logger.LogInformation("Unified Meta webhook enqueued: {EventKey} Connection={ConnectionId} Channel={Channel}",
                eventKey, connection.Id, channel);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving unified Meta webhook");
            return Ok();
        }
    }

    private static bool TryResolveRouting(string rawBody, out MetaChannel channel, out string identityValue)
    {
        channel = default;
        identityValue = string.Empty;

        using var doc = System.Text.Json.JsonDocument.Parse(rawBody);

        if (!doc.RootElement.TryGetProperty("object", out var objectElement))
        {
            return false;
        }

        var objectType = objectElement.GetString();
        if (string.IsNullOrWhiteSpace(objectType))
        {
            return false;
        }

        if (!doc.RootElement.TryGetProperty("entry", out var entries) || entries.ValueKind != System.Text.Json.JsonValueKind.Array || entries.GetArrayLength() == 0)
        {
            return false;
        }

        var firstEntry = entries[0];

        // Messenger (Facebook Page)
        if (string.Equals(objectType, "page", StringComparison.OrdinalIgnoreCase))
        {
            channel = MetaChannel.Messenger;
            identityValue = firstEntry.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
            return !string.IsNullOrWhiteSpace(identityValue);
        }

        // Instagram
        if (string.Equals(objectType, "instagram", StringComparison.OrdinalIgnoreCase))
        {
            channel = MetaChannel.Instagram;
            identityValue = firstEntry.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
            return !string.IsNullOrWhiteSpace(identityValue);
        }

        // WhatsApp Cloud API
        if (string.Equals(objectType, "whatsapp_business_account", StringComparison.OrdinalIgnoreCase))
        {
            channel = MetaChannel.WhatsApp;

            // Preferred: metadata.phone_number_id (matches DB field WhatsAppPhoneNumberId)
            if (firstEntry.TryGetProperty("changes", out var changes) && changes.ValueKind == System.Text.Json.JsonValueKind.Array && changes.GetArrayLength() > 0)
            {
                var firstChange = changes[0];
                if (firstChange.TryGetProperty("value", out var value)
                    && value.TryGetProperty("metadata", out var metadata)
                    && metadata.TryGetProperty("phone_number_id", out var phoneIdEl))
                {
                    identityValue = phoneIdEl.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(identityValue);
                }
            }

            // Fallback: entry.id (often WABA id) - not routable in current schema
            identityValue = string.Empty;
            return false;
        }

        return false;
    }

    private static string? TryExtractExternalUserId(string rawBody, MetaChannel channel)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawBody);

            if (!doc.RootElement.TryGetProperty("entry", out var entries)
                || entries.ValueKind != System.Text.Json.JsonValueKind.Array
                || entries.GetArrayLength() == 0)
            {
                return null;
            }

            var firstEntry = entries[0];

            // Messenger + Instagram use "messaging" array with sender.id
            if (channel == MetaChannel.Messenger || channel == MetaChannel.Instagram)
            {
                if (firstEntry.TryGetProperty("messaging", out var messaging)
                    && messaging.ValueKind == System.Text.Json.JsonValueKind.Array
                    && messaging.GetArrayLength() > 0)
                {
                    var firstMsg = messaging[0];
                    if (firstMsg.TryGetProperty("sender", out var sender)
                        && sender.TryGetProperty("id", out var senderId))
                    {
                        var id = senderId.GetString();
                        return string.IsNullOrWhiteSpace(id) ? null : id;
                    }
                }

                return null;
            }

            // WhatsApp: entry[0].changes[0].value.messages[0].from
            if (channel == MetaChannel.WhatsApp)
            {
                if (firstEntry.TryGetProperty("changes", out var changes)
                    && changes.ValueKind == System.Text.Json.JsonValueKind.Array
                    && changes.GetArrayLength() > 0)
                {
                    var firstChange = changes[0];
                    if (firstChange.TryGetProperty("value", out var value)
                        && value.TryGetProperty("messages", out var messages)
                        && messages.ValueKind == System.Text.Json.JsonValueKind.Array
                        && messages.GetArrayLength() > 0)
                    {
                        var firstMessage = messages[0];
                        if (firstMessage.TryGetProperty("from", out var from))
                        {
                            var id = from.GetString();
                            return string.IsNullOrWhiteSpace(id) ? null : id;
                        }
                    }
                }

                return null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
