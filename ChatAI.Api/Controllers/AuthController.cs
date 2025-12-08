using ChatAI.Application.Commands;
using ChatAI.Application.DTOs;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

        var response = await _mediator.Send(command);

        // Set authentication cookie for browser-based access
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, response.Username),
            new Claim(ClaimTypes.Name, response.Username),
            new Claim(ClaimTypes.Email, response.Email ?? ""),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = response.ExpiresAt
            });

        _logger.LogInformation("Admin logged in successfully: {Username}", request.Username);

        return Ok(response);
    }

    /// <summary>
    /// Admin logout endpoint
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = "Admin")]
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
    [Authorize(Policy = "Admin")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            username = User.Identity?.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            role = User.FindFirst(ClaimTypes.Role)?.Value,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }

    /// <summary>
    /// Create a new API key (Admin only)
    /// </summary>
    [HttpPost("api-keys")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiKeyResponseDto>> CreateApiKey([FromBody] CreateApiKeyDto request)
    {
        // Get admin user ID from claims
        var username = User.Identity?.Name ?? "system";
        
        var command = new CreateApiKeyCommand
        {
            ClientName = request.ClientName,
            Description = request.Description,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = Guid.Empty // TODO: Get actual admin user ID from claims
        };

        var response = await _mediator.Send(command);

        _logger.LogInformation("API key created by {Username} for client: {ClientName}", username, request.ClientName);

        return Ok(response);
    }

    /// <summary>
    /// Get all API keys (Admin only)
    /// </summary>
    [HttpGet("api-keys")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<List<ApiKeyResponseDto>>> GetApiKeys([FromQuery] bool includeInactive = false)
    {
        var query = new GetApiKeysQuery { IncludeInactive = includeInactive };
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    /// <summary>
    /// Revoke an API key (Admin only)
    /// </summary>
    [HttpDelete("api-keys/{keyId}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> RevokeApiKey(Guid keyId)
    {
        var command = new RevokeApiKeyCommand
        {
            KeyId = keyId,
            RevokedBy = Guid.Empty // TODO: Get actual admin user ID from claims
        };

        var result = await _mediator.Send(command);

        if (result)
        {
            _logger.LogInformation("API key revoked by {Username}: {KeyId}", User.Identity?.Name, keyId);
            return Ok(new { message = "API key revoked successfully" });
        }

        return NotFound(new { message = "API key not found" });
    }
}
