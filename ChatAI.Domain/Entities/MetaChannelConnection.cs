namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a tenant's connection to a Meta channel (Messenger, Instagram, WhatsApp)
/// Uses shared Meta App with unified webhook endpoint (/api/webhooks/meta)
/// Stores encrypted credentials and channel-specific identifiers
/// </summary>
public class MetaChannelConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Tenant that owns this connection
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Channel type
    /// </summary>
    public Enums.MetaChannel Channel { get; set; }
    
    /// <summary>
    /// Legacy webhook identifier (retained for backward compatibility)
    /// Modern shared app uses unified webhook: /api/webhooks/meta
    /// </summary>
    public Guid WebhookId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Hashed verify token for webhook GET verification handshake
    /// </summary>
    public string VerifyTokenHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Plain verify token (temporarily stored for display to tenant during setup)
    /// Should be cleared after tenant confirms setup
    /// </summary>
    public string? VerifyTokenPlain { get; set; }
    
    /// <summary>
    /// Tenant's Meta App ID (from Meta Developer dashboard)
    /// Used for reference and validation
    /// </summary>
    public string MetaAppId { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant's Meta App Secret (encrypted)
    /// Used for webhook signature validation (X-Hub-Signature-256)
    /// </summary>
    public string MetaAppSecretEncrypted { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant's access token (encrypted) for sending messages via Graph API
    /// Can be Page token, IG account token, or WhatsApp Cloud API token
    /// </summary>
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    
    /// <summary>
    /// Version of the encryption key used (for key rotation)
    /// </summary>
    public int TokenKeyVersion { get; set; } = 1;
    
    /// <summary>
    /// How the token was obtained: Manual entry or OAuth flow
    /// </summary>
    public Enums.TokenSource TokenSource { get; set; } = Enums.TokenSource.Manual;
    
    /// <summary>
    /// OAuth refresh token (encrypted, nullable)
    /// Only available for some Meta token types
    /// </summary>
    public string? OAuthRefreshTokenEncrypted { get; set; }
    
    /// <summary>
    /// OAuth scopes granted by user during authorization
    /// Stored as JSON array: ["pages_messaging", "pages_manage_metadata"]
    /// </summary>
    public string? OAuthScopes { get; set; }
    
    /// <summary>
    /// System User ID (for never-expiring tokens)
    /// Only available with Meta Business Manager
    /// </summary>
    public string? SystemUserId { get; set; }
    
    // Channel-specific identity fields (nullable based on channel type)
    
    /// <summary>
    /// Facebook Page ID (for Messenger)
    /// Must be globally unique
    /// </summary>
    public string? FacebookPageId { get; set; }
    
    /// <summary>
    /// Instagram Business Account ID (for Instagram)
    /// Must be globally unique
    /// </summary>
    public string? InstagramBusinessAccountId { get; set; }
    
    /// <summary>
    /// WhatsApp Phone Number ID (for WhatsApp Cloud API)
    /// Must be globally unique
    /// </summary>
    public string? WhatsAppPhoneNumberId { get; set; }
    
    /// <summary>
    /// WhatsApp Business Account ID (for reference)
    /// </summary>
    public string? WhatsAppBusinessAccountId { get; set; }
    
    // State and monitoring
    
    /// <summary>
    /// Whether this connection is active and receiving webhooks
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Last time a webhook was received
    /// </summary>
    public DateTime? LastWebhookAt { get; set; }
    
    /// <summary>
    /// Last time the token was validated successfully
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }
    
    /// <summary>
    /// Last time a message was sent successfully
    /// </summary>
    public DateTime? LastSendAt { get; set; }
    
    /// <summary>
    /// Last error message (sanitized, no PII)
    /// </summary>
    public string? LastError { get; set; }
    
    /// <summary>
    /// When the last error occurred
    /// </summary>
    public DateTime? LastErrorAt { get; set; }
    
    /// <summary>
    /// Count of consecutive failed send attempts
    /// Reset to 0 on successful send
    /// Connection auto-disabled at threshold (10)
    /// </summary>
    public int FailedSendCount { get; set; } = 0;
    
    /// <summary>
    /// When the token expires (from debug_token endpoint)
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Warning flag: token expires within 7 days
    /// </summary>
    public bool TokenExpiryWarning { get; set; } = false;
    
    /// <summary>
    /// When token expiry was detected
    /// </summary>
    public DateTime? TokenExpiredAt { get; set; }
    
    /// <summary>
    /// When this connection was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this connection was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
