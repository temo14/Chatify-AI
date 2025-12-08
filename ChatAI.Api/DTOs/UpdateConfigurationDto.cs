using System.ComponentModel.DataAnnotations;

namespace ChatAI.Api.DTOs;

/// <summary>
/// DTO for creating/updating configuration
/// </summary>
public class UpdateConfigurationDto
{
    [MaxLength(200)]
    public string? Key { get; set; }

    [Required]
    public string Value { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DataType { get; set; } = "String"; // String, Integer, Boolean, JSON

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? ModifiedBy { get; set; }

    [MaxLength(200)]
    public string? ValidationRule { get; set; }
}
