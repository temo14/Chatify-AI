namespace ChatAI.Domain.Models.MetaOAuth;

using System.Text.Json.Serialization;

/// <summary>
/// Meta Graph API token exchange response
/// </summary>
public record OAuthTokenResponse
{
    /// <summary>
    /// Access token to use for API calls
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;
    
    /// <summary>
    /// Token type (always "bearer" for Meta)
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "bearer";
    
    /// <summary>
    /// Token lifetime in seconds (e.g., 5183999 for 60 days)
    /// </summary>
    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; init; }
    
    /// <summary>
    /// Absolute expiration time
    /// </summary>
    public DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(ExpiresIn);
    
    /// <summary>
    /// Refresh token (not all Meta tokens support this)
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
