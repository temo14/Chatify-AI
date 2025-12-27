using System.Security.Claims;
using System.Text.Encodings.Web;
using ChatAI.Application.Features.Auth.ValidateApiKey;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Custom authentication handler for API key authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IMediator _mediator;
    private const string API_KEY_HEADER = "X-API-Key";
    
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMediator mediator)
        : base(options, logger, encoder)
    {
        _mediator = mediator;
    }
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key header exists
        if (!Request.Headers.TryGetValue(API_KEY_HEADER, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }
        
        var apiKey = apiKeyHeader.ToString();
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }
        
        try
        {
            // Validate API key using MediatR
            var validatedKey = await _mediator.Send(new ValidateApiKeyQuery { ApiKey = apiKey });
            
            if (validatedKey == null)
            {
                return AuthenticateResult.Fail("Invalid API key");
            }
            
            // Create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, validatedKey.Id.ToString()),
                new Claim(ClaimTypes.Name, validatedKey.ClientName),
                new Claim("tenant_id", validatedKey.TenantId),
                new Claim(ClaimTypes.Role, "Client")
            };
            
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key");
            return AuthenticateResult.Fail("API key validation failed");
        }
    }
}
