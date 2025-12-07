using System.ComponentModel.DataAnnotations;

namespace ChatAI.Api.DTOs.Knowledge;

/// <summary>
/// Request to add a new knowledge document
/// </summary>
public class AddKnowledgeDocumentRequest
{
    /// <summary>
    /// Document title (required, 1-500 characters)
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 500 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Document content (required, 1-50000 characters)
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [StringLength(50000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 50000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source of document (optional, max 500 characters)
    /// </summary>
    [StringLength(500, ErrorMessage = "Source must not exceed 500 characters")]
    public string? Source { get; set; }

    /// <summary>
    /// Category for organization (optional, max 100 characters)
    /// </summary>
    [StringLength(100, ErrorMessage = "Category must not exceed 100 characters")]
    public string? Category { get; set; }

    /// <summary>
    /// Additional metadata as JSON (optional)
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Whether document is active (default: true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
