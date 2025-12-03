using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatAI.Api.Middleware;

/// <summary>
/// Simple API Key authentication middleware for development/testing
/// For production, use proper JWT/OAuth authentication
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // Skip auth for Swagger and health checks
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/swagger") || path.Contains("/openapi") || path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // Check if API key is provided
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API Key missing from request");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing", hint = "Add 'X-API-Key' header with your API key" });
            return;
        }

        // Validate API key (in production, check against database)
        var validApiKeys = configuration.GetSection("ApiKeys").Get<Dictionary<string, string>>() 
                          ?? new Dictionary<string, string>();

        var apiKey = extractedApiKey.ToString();
        var user = validApiKeys.FirstOrDefault(k => k.Value == apiKey);

        if (user.Key == null)
        {
            _logger.LogWarning("Invalid API Key: {ApiKey}", apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "...");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        // Set user identity in HttpContext
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Key),
            new Claim(ClaimTypes.Name, user.Key),
            new Claim("ApiKey", apiKey)
        };

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        _logger.LogInformation("Authenticated user: {UserId}", user.Key);

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
