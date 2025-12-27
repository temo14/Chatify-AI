namespace ChatAI.Domain.Entities;

/// <summary>
/// API key entity for third-party client authentication
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// SHA256 hash of the actual API key (never store plain key)
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name for the client
    /// </summary>
    public string ClientName { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant identifier - associates this API key with a specific tenant
    /// Stored as string representation of Guid
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this key is used for
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Rate limit per minute for this specific key
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 20;
    
    /// <summary>
    /// Rate limit per day for this specific key
    /// </summary>
    public int RateLimitPerDay { get; set; } = 1000;
    
    /// <summary>
    /// When the key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the key expires (null for no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Last time this key was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Total number of API calls made with this key
    /// </summary>
    public long UsageCount { get; set; } = 0;
    
    /// <summary>
    /// Admin user who created this key
    /// </summary>
    public Guid? CreatedBy { get; set; }
}
