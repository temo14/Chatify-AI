namespace ChatAI.Domain.Models.MetaOAuth;

/// <summary>
/// Encrypted state parameter for OAuth CSRF protection.
/// Stored in distributed cache during OAuth flow.
/// </summary>
public record OAuthState
{
    /// <summary>
    /// Server-generated transaction identifier (used as cache key)
    /// </summary>
    public Guid TransactionId { get; init; }

    /// <summary>
    /// Tenant initiating the OAuth flow
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Tenant user who initiated the OAuth flow (binds completion to authenticated user)
    /// </summary>
    public string InitiatingUserId { get; init; } = string.Empty;
    
    /// <summary>
    /// Channel being connected (Messenger/Instagram/WhatsApp)
    /// </summary>
    public Enums.MetaChannel Channel { get; init; }
    
    /// <summary>
    /// When the OAuth flow was initiated
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Random nonce for additional entropy (32-byte base64)
    /// </summary>
    public string Nonce { get; init; } = string.Empty;
    
    /// <summary>
    /// Validates state is not expired (10 minute window)
    /// </summary>
    public bool IsValid() => DateTime.UtcNow - Timestamp < TimeSpan.FromMinutes(10);
}
