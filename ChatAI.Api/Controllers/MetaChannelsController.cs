using ChatAI.Api.Attributes;
using ChatAI.Api.DTOs;
using ChatAI.Api.DTOs.MetaOAuth;
using ChatAI.Application.Features.MetaChannels.CreateConnection;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Meta channels management for tenant admins
/// </summary>
[ApiController]
[Route("api/tenant/meta")]
[Authorize]
[TenantAdmin]
public class MetaChannelsController : ControllerBase
{
    private readonly IMetaChannelConnectionRepository _connectionRepository;
    private readonly IMetaTokenValidator _tokenValidator;
    private readonly IEncryptionService _encryptionService;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaChannelsController> _logger;
    
    public MetaChannelsController(
        IMetaChannelConnectionRepository connectionRepository,
        IMetaTokenValidator tokenValidator,
        IEncryptionService encryptionService,
        IMediator mediator,
        IConfiguration configuration,
        ILogger<MetaChannelsController> logger)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// List all Meta connections for current tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MetaConnectionResponseDto>>> GetConnections()
    {
        var connections = await _connectionRepository.GetAllForTenantAsync();
        
        // Single shared webhook endpoint for the entire platform (one shared Meta App)
        // Force HTTPS (Request.Scheme returns http behind Azure proxy)
        var webhookUrl = $"https://{Request.Host}/api/webhooks/meta";
        
        var result = connections.Select(c =>
        {
            // Show verify token if connection was created within last 24 hours (for setup)
            var showVerifyToken = (DateTime.UtcNow - c.CreatedAt).TotalHours < 24 && !string.IsNullOrEmpty(c.VerifyTokenPlain);
            
            return new MetaConnectionResponseDto
            {
                Id = c.Id,
                Channel = c.Channel,
                // Deprecated in OAuth-only, shared-webhook architecture
                WebhookId = Guid.Empty,
                WebhookUrl = webhookUrl,
                VerifyToken = "(configured in Meta App Settings)",
                MetaAppId = c.MetaAppId,
                IsActive = c.IsActive,
                LastWebhookAt = c.LastWebhookAt,
                LastValidatedAt = c.LastValidatedAt,
                LastSendAt = c.LastSendAt,
                LastError = c.LastError,
                LastErrorAt = c.LastErrorAt,
                FailedSendCount = c.FailedSendCount,
                TokenExpiresAt = c.TokenExpiresAt,
                TokenExpiryWarning = c.TokenExpiryWarning,
                CreatedAt = c.CreatedAt,
                FacebookPageId = c.FacebookPageId,
                InstagramBusinessAccountId = c.InstagramBusinessAccountId,
                WhatsAppPhoneNumberId = c.WhatsAppPhoneNumberId
            };
        }).ToList();
        
        return Ok(result);
    }
    
    /// <summary>
    /// Get a specific connection
    /// </summary>
    [HttpGet("{connectionId:guid}")]
    public async Task<ActionResult<MetaConnectionResponseDto>> GetConnection(Guid connectionId)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        if (connection == null)
        {
            return NotFound();
        }
        
        // Single shared webhook endpoint for the entire platform (one shared Meta App)
        // Force HTTPS (Request.Scheme returns http behind Azure proxy)
        var webhookUrl = $"https://{Request.Host}/api/webhooks/meta";
        
        // Show verify token if connection was created within last 24 hours (for setup)
        var showVerifyToken = (DateTime.UtcNow - connection.CreatedAt).TotalHours < 24 && !string.IsNullOrEmpty(connection.VerifyTokenPlain);
        
        var result = new MetaConnectionResponseDto
        {
            Id = connection.Id,
            Channel = connection.Channel,
            // Deprecated in OAuth-only, shared-webhook architecture
            WebhookId = Guid.Empty,
            WebhookUrl = webhookUrl,
            VerifyToken = "(configured in Meta App Settings)",
            MetaAppId = connection.MetaAppId,
            IsActive = connection.IsActive,
            LastWebhookAt = connection.LastWebhookAt,
            LastValidatedAt = connection.LastValidatedAt,
            LastSendAt = connection.LastSendAt,
            LastError = connection.LastError,
            LastErrorAt = connection.LastErrorAt,
            FailedSendCount = connection.FailedSendCount,
            TokenExpiresAt = connection.TokenExpiresAt,
            TokenExpiryWarning = connection.TokenExpiryWarning,
            CreatedAt = connection.CreatedAt,
            FacebookPageId = connection.FacebookPageId,
            InstagramBusinessAccountId = connection.InstagramBusinessAccountId,
            WhatsAppPhoneNumberId = connection.WhatsAppPhoneNumberId
        };
        
        return Ok(result);
    }
    
    /// <summary>
    /// Create a new Meta channel connection
    /// </summary>
    [HttpPost("{channel}/connect")]
    public async Task<ActionResult<MetaConnectionResponseDto>> CreateConnection(
        [FromRoute] string channel,
        [FromBody] CreateMetaConnectionDto dto)
    {
        // SECURITY/POLICY: Multi-tenant SaaS must use ONE shared Meta App.
        // Tenants must NOT provide their own App ID/Secret or manual tokens.
        return BadRequest(new
        {
            error = "Manual Meta connections are disabled. Use OAuth via /api/tenant/meta/{channel}/oauth/initiate and /api/tenant/meta/oauth/complete.",
            code = "MANUAL_CONNECTIONS_DISABLED"
        });
    }
    
    /// <summary>
    /// Validate a connection's access token
    /// </summary>
    [HttpPost("{connectionId:guid}/validate")]
    public async Task<ActionResult<MetaConnectionValidationResultDto>> ValidateConnection(Guid connectionId)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        if (connection == null)
        {
            return NotFound();
        }
        
        var accessToken = _encryptionService.Decrypt(connection.AccessTokenEncrypted, connection.TokenKeyVersion);
        var validationResult = await _tokenValidator.ValidateTokenAsync(accessToken);
        
        // Update connection with validation results
        connection.LastValidatedAt = DateTime.UtcNow;
        connection.TokenExpiresAt = validationResult.ExpiresAt;
        
        if (validationResult.ExpiresAt.HasValue)
        {
            var daysUntilExpiry = (validationResult.ExpiresAt.Value - DateTime.UtcNow).TotalDays;
            connection.TokenExpiryWarning = daysUntilExpiry <= 7;
        }
        
        if (!validationResult.IsValid)
        {
            connection.IsActive = false;
            connection.TokenExpiredAt = DateTime.UtcNow;
            connection.LastError = "Token validation failed";
            connection.LastErrorAt = DateTime.UtcNow;
        }
        
        await _connectionRepository.UpdateAsync(connection);
        
        var result = new MetaConnectionValidationResultDto
        {
            IsValid = validationResult.IsValid,
            ErrorMessage = validationResult.ErrorMessage,
            TokenExpiresAt = validationResult.ExpiresAt,
            Scopes = validationResult.Scopes
        };
        
        return Ok(result);
    }
    
    /// <summary>
    /// Rotate access token
    /// </summary>
    [HttpPost("{connectionId:guid}/rotate-token")]
    public async Task<IActionResult> RotateToken(
        Guid connectionId,
        [FromBody] RotateMetaTokenDto dto)
    {
        // SECURITY/POLICY: Manual token rotation is not supported in OAuth-only architecture.
        // Token lifecycle is managed via re-authentication.
        return BadRequest(new
        {
            error = "Manual token rotation is disabled. Reconnect the asset via OAuth.",
            code = "TOKEN_ROTATION_DISABLED"
        });
    }
    
    /// <summary>
    /// Disconnect (delete) a Meta channel connection
    /// </summary>
    [HttpDelete("{connectionId:guid}")]
    public async Task<IActionResult> Disconnect(Guid connectionId)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        if (connection == null)
        {
            return NotFound();
        }
        
        await _connectionRepository.DeleteAsync(connectionId);
        
        _logger.LogInformation("Disconnected Meta connection {ConnectionId}", connectionId);
        
        return NoContent();
    }
    
    /// <summary>
    /// Initiate OAuth flow for a Meta channel
    /// </summary>
    [HttpPost("{channel}/oauth/initiate")]
    public async Task<ActionResult<OAuthInitiateResponseDto>> InitiateOAuth(
        [FromRoute] string channel,
        [FromBody] OAuthInitiateRequestDto request)
    {
        if (!Enum.TryParse<MetaChannel>(channel, ignoreCase: true, out var metaChannel))
        {
            return BadRequest(new { error = "Invalid channel" });
        }
        
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
        {
            return Unauthorized();
        }

        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        var command = new Application.Features.MetaChannels.OAuth.InitiateOAuthCommand(
            TenantId: tenantGuid,
            InitiatingUserId: userId,
            Channel: metaChannel);
        
        var result = await _mediator.Send(command);
        
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        
        return Ok(new OAuthInitiateResponseDto
        {
            AuthorizationUrl = result.AuthorizationUrl!,
            State = result.State!
        });
    }
    
    /// <summary>
    /// OAuth callback endpoint - redirects user after Meta authorization
    /// </summary>
    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        // Handle Meta OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth error: {Error} - {Description}", error, error_description);
            
            // Sanitize error description for URL
            var sanitizedError = SanitizeForUrl(error_description ?? error);
            return Redirect($"/admin.html#oauth-error={sanitizedError}");
        }
        
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("OAuth callback missing required parameters");
            return Redirect("/admin.html#oauth-error=missing_parameters");
        }
        
        // Validate parameter lengths (defense against attacks)
        if (code.Length > 1000 || state.Length > 500)
        {
            _logger.LogWarning("OAuth callback parameters exceed maximum length");
            return Redirect("/admin.html#oauth-error=invalid_parameters");
        }
        
        // IMPORTANT: Do not complete OAuth server-side on an anonymous callback.
        // We redirect back to the UI, which must call the authenticated completion endpoint.
        var uiUrl = $"/admin.html#meta-oauth-callback=true&code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}";
        return Redirect(uiUrl);
    }

    /// <summary>
    /// Complete OAuth flow (authenticated) - enforces tenant/user binding.
    /// The UI calls this after handling the Meta redirect.
    /// </summary>
    [HttpPost("oauth/complete")]
    public async Task<ActionResult<OAuthCallbackResultDto>> CompleteOAuth([FromBody] OAuthCompleteRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.State))
        {
            return BadRequest(new OAuthCallbackResultDto { Success = false, ErrorCode = "VALIDATION_FAILED", ErrorMessage = "Missing code/state" });
        }

        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
        {
            return Unauthorized();
        }

        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // The redirect URI used for token exchange must match the configured redirect URI in the Meta App.
        var redirectUri = _configuration["Meta:OAuth:RedirectUri"]
            ?? throw new InvalidOperationException("Meta:OAuth:RedirectUri not configured");

        var command = new Application.Features.MetaChannels.OAuth.CompleteOAuthCommand(
            TenantId: tenantGuid,
            InitiatingUserId: userId,
            Code: dto.Code,
            State: dto.State,
            RedirectUri: redirectUri);

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(new OAuthCallbackResultDto
            {
                Success = false,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            });
        }

        return Ok(new OAuthCallbackResultDto
        {
            Success = true,
            ConnectionId = result.ConnectionId,
            // Single webhook endpoint for the whole app
            WebhookUrl = $"https://{Request.Host}/api/webhooks/meta",
            VerifyToken = null
        });
    }
    
    /// <summary>
    /// Sanitize error messages for safe URL inclusion
    /// </summary>
    private static string SanitizeForUrl(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown_error";
        
        // Limit length
        if (input.Length > 200)
            input = input.Substring(0, 200);
        
        // URL encode for safe inclusion in fragment
        return Uri.EscapeDataString(input);
    }
}
