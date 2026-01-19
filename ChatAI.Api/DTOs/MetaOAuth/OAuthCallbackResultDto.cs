namespace ChatAI.Api.DTOs.MetaOAuth;

/// <summary>
/// Result of OAuth callback completion
/// </summary>
public record OAuthCallbackResultDto
{
    /// <summary>
    /// Whether OAuth flow completed successfully
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// ID of the created Meta channel connection
    /// </summary>
    public Guid? ConnectionId { get; init; }
    
    /// <summary>
    /// Webhook URL to configure in Meta Developer Console
    /// </summary>
    public string? WebhookUrl { get; init; }
    
    /// <summary>
    /// Verify token for webhook handshake (shown once)
    /// </summary>
    public string? VerifyToken { get; init; }
    
    /// <summary>
    /// Error message if OAuth failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; init; }
}
