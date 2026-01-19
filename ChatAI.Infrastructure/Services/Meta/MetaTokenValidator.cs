using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Service for validating Meta access tokens using debug_token endpoint
/// </summary>
public class MetaTokenValidator : IMetaTokenValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MetaTokenValidator> _logger;
    private readonly string _graphApiVersion;
    
    public MetaTokenValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MetaTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphApiVersion = configuration["Meta:GraphApiVersion"] ?? "v22.0";
    }
    
    public async Task<MetaTokenValidationResult> ValidateTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var result = new MetaTokenValidationResult();
        
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            // Use the token itself to debug itself (for user/page tokens)
            var url = $"https://graph.facebook.com/{_graphApiVersion}/debug_token?input_token={accessToken}&access_token={accessToken}";
            
            var response = await client.GetAsync(url, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonDocument.Parse(responseContent);
                var data = jsonResponse.RootElement.GetProperty("data");
                
                result.IsValid = data.GetProperty("is_valid").GetBoolean();
                
                if (result.IsValid)
                {
                    // Get expiry time if available
                    if (data.TryGetProperty("expires_at", out var expiresAtElement) && expiresAtElement.ValueKind != JsonValueKind.Null)
                    {
                        var expiresAtUnix = expiresAtElement.GetInt64();
                        if (expiresAtUnix > 0)
                        {
                            result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;
                        }
                    }
                    
                    // Get scopes
                    if (data.TryGetProperty("scopes", out var scopesElement))
                    {
                        result.Scopes = scopesElement.EnumerateArray()
                            .Select(s => s.GetString()!)
                            .ToList();
                    }
                    
                    // Get app ID
                    if (data.TryGetProperty("app_id", out var appIdElement))
                    {
                        result.AppId = appIdElement.GetString();
                    }
                    
                    _logger.LogInformation("Token validated successfully, expires: {ExpiresAt}", result.ExpiresAt);
                }
                else
                {
                    result.ErrorMessage = "Token is invalid";
                    _logger.LogWarning("Token validation failed: token is invalid");
                }
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}";
                _logger.LogError("Token validation API call failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exception validating token");
        }
        
        return result;
    }
}
