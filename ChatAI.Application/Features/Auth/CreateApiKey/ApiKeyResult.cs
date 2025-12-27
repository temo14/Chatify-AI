namespace ChatAI.Application.Features.Auth.CreateApiKey;

/// <summary>
/// Result returned after creating an API key
/// </summary>
public class ApiKeyResult
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int RateLimitPerMinute { get; set; }
    public int RateLimitPerDay { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public long UsageCount { get; set; }
    
    /// <summary>
    /// Plain text API key - ONLY populated on creation, null otherwise
    /// </summary>
    public string? ApiKey { get; set; }
}
