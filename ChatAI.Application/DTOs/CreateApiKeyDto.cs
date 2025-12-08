namespace ChatAI.Application.DTOs;

/// <summary>
/// Request DTO for creating a new API key
/// </summary>
public class CreateApiKeyDto
{
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RateLimitPerMinute { get; set; } = 20;
    public int RateLimitPerDay { get; set; } = 1000;
    public DateTime? ExpiresAt { get; set; }
}
