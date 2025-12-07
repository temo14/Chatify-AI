namespace ChatAI.Domain.Entities;

/// <summary>
/// Admin-configurable system settings
/// Allows runtime configuration without redeploying
/// </summary>
public class AdminConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Configuration key (e.g., "AI.SystemPrompt", "Features.EmailEnabled")
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Configuration value (stored as JSON for flexibility)
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Data type: String, Integer, Boolean, JSON
    /// </summary>
    public string DataType { get; set; } = "String";
    
    /// <summary>
    /// Category for grouping (AI, Features, Security, Branding, etc.)
    /// </summary>
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Human-readable description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this setting is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Who last modified this setting
    /// </summary>
    public string? ModifiedBy { get; set; }
    
    /// <summary>
    /// When it was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification time
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Validation rules (regex or JSON schema)
    /// </summary>
    public string? ValidationRule { get; set; }
}
