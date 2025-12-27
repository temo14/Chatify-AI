using ChatAI.Api.Attributes;
using ChatAI.Api.DTOs;
using ChatAI.Application.Features.AdminUsers.ResetPassword;
using ChatAI.Application.Features.Auth.CreateApiKey;
using ChatAI.Application.Features.Auth.GetApiKeys;
using ChatAI.Application.Features.Auth.Login;
using ChatAI.Application.Features.Auth.RevokeApiKey;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Admin login endpoint - returns JWT token and sets cookie
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto request)
    {
        var command = new LoginCommand
        {
            Username = request.Username,
            Password = request.Password,
            RememberMe = request.RememberMe
        };

        var result = await _mediator.Send(command);

        // Map domain result to response DTO
        var response = new LoginResponseDto
        {
            Username = result.Username,
            FullName = result.FullName,
            Email = result.Email,
            Token = result.Token,
            ExpiresAt = result.ExpiresAt
        };

        // Set authentication cookie for browser-based access
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.Username),
            new Claim(ClaimTypes.Name, result.Username),
            new Claim(ClaimTypes.Email, result.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, result.Role),
            new Claim("tenant_id", result.TenantId.ToString()),
            new Claim("user_id", result.UserId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = result.ExpiresAt
            });

        _logger.LogInformation("Admin logged in successfully: {Username}", request.Username);

        return Ok(response);
    }

    /// <summary>
    /// Admin logout endpoint
    /// </summary>
    [HttpPost("logout")]
    [TenantAdmin]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Admin logged out: {Username}", User.Identity?.Name);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [TenantAdmin]
    public IActionResult GetCurrentUser()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst("user_id")?.Value;
        
        return Ok(new
        {
            userId = userId,
            username = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            role = role,
            tenantId = tenantId,
            isPlatformAdmin = role == "PlatformAdmin",
            isTenantAdmin = role == "TenantAdmin",
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            permissions = new
            {
                canManageTenants = role == "PlatformAdmin",
                canManageGlobalConfig = role == "PlatformAdmin",
                canManageKnowledgeBase = true, // Both roles
                canManageChatSettings = true, // Both roles
                canViewAnalytics = true // Both roles
            }
        });
    }

    /// <summary>
    /// Create a new API key (Admin only)
    /// </summary>
    [HttpPost("api-keys")]
    [TenantAdmin]
    public async Task<ActionResult<ApiKeyResponseDto>> CreateApiKey([FromBody] CreateApiKeyDto request)
    {
        // Get admin user ID from JWT claims (Sub claim contains user ID)
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var adminUserId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : Guid.Empty;
        
        // Get tenant ID from JWT claims (required for tenant-scoped API keys)
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest(new { message = "Invalid tenant context. Admin must be associated with a tenant." });
        }
        
        var command = new CreateApiKeyCommand
        {
            ClientName = request.ClientName,
            Description = request.Description,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = adminUserId,
            TenantId = tenantId
        };

        var result = await _mediator.Send(command);

        // Map domain result to response DTO
        var response = new ApiKeyResponseDto
        {
            Id = result.Id,
            ClientName = result.ClientName,
            TenantId = result.TenantId,
            Description = result.Description,
            IsActive = result.IsActive,
            RateLimitPerMinute = result.RateLimitPerMinute,
            RateLimitPerDay = result.RateLimitPerDay,
            CreatedAt = result.CreatedAt,
            ExpiresAt = result.ExpiresAt,
            LastUsedAt = result.LastUsedAt,
            UsageCount = result.UsageCount,
            ApiKey = result.ApiKey
        };

        _logger.LogInformation("API key created by admin {AdminUserId} for client: {ClientName}", 
            adminUserId, request.ClientName);

        return Ok(response);
    }

    /// <summary>
    /// Get all API keys (Admin only)
    /// </summary>
    [HttpGet("api-keys")]
    [TenantAdmin]
    public async Task<ActionResult<List<ApiKeyResponseDto>>> GetApiKeys([FromQuery] bool includeInactive = false)
    {
        var query = new GetApiKeysQuery { IncludeInactive = includeInactive };
        var apiKeys = await _mediator.Send(query);
        
        // Map domain entities to response DTOs
        var response = apiKeys.Select(k => new ApiKeyResponseDto
        {
            Id = k.Id,
            ClientName = k.ClientName,
            TenantId = k.TenantId,
            Description = k.Description,
            IsActive = k.IsActive,
            RateLimitPerMinute = k.RateLimitPerMinute,
            RateLimitPerDay = k.RateLimitPerDay,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            UsageCount = k.UsageCount,
            ApiKey = null // Never return plain key after creation
        }).ToList();
        
        return Ok(response);
    }

    /// <summary>
    /// Revoke an API key (Admin only)
    /// </summary>
    [HttpDelete("api-keys/{keyId}")]
    [TenantAdmin]
    public async Task<IActionResult> RevokeApiKey(Guid keyId)
    {
        // Get admin user ID from JWT claims (Sub claim contains user ID)
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var adminUserId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : Guid.Empty;
        
        var command = new RevokeApiKeyCommand
        {
            KeyId = keyId,
            RevokedBy = adminUserId
        };

        var result = await _mediator.Send(command);

        if (result)
        {
            _logger.LogInformation("API key revoked by {Username}: {KeyId}", User.Identity?.Name, keyId);
            return Ok(new { message = "API key revoked successfully" });
        }

        return NotFound(new { message = "API key not found" });
    }

    /// <summary>
    /// Reset user password (Platform Admin only)
    /// </summary>
    [HttpPost("reset-password/{userId}")]
    [PlatformAdmin]
    public async Task<ActionResult> ResetUserPassword(Guid userId, [FromBody] ResetPasswordDto request)
    {
        await _mediator.Send(new ResetUserPasswordCommand 
        { 
            UserId = userId, 
            NewPassword = request.NewPassword 
        });
        return Ok(new { message = "Password reset successfully" });
    }
}
