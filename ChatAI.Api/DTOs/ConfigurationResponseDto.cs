namespace ChatAI.Api.DTOs;

/// <summary>
/// DTO for configuration response
/// </summary>
public class ConfigurationResponseDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ValidationRule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
