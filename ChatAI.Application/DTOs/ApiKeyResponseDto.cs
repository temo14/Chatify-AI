namespace ChatAI.Application.DTOs;

/// <summary>
/// Response DTO for API key (includes plain key only on creation)
/// </summary>
public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
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
