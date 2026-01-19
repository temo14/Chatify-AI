namespace ChatAI.Api.DTOs.MetaOAuth;

/// <summary>
/// Response from OAuth initiation with authorization URL
/// </summary>
public record OAuthInitiateResponseDto
{
    /// <summary>
    /// Full Meta OAuth authorization URL to redirect user to
    /// </summary>
    public string AuthorizationUrl { get; init; } = string.Empty;
    
    /// <summary>
    /// Encrypted state parameter (for client-side validation if needed)
    /// </summary>
    public string State { get; init; } = string.Empty;
}
