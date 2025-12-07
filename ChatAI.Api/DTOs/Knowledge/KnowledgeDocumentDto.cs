using System.ComponentModel.DataAnnotations;

namespace ChatAI.Api.DTOs.Knowledge;

/// <summary>
/// DTO for knowledge document responses (API layer only)
/// Mapping logic moved to Application layer (KnowledgeDocumentResponse)
/// </summary>
public class KnowledgeDocumentDto
{
    public Guid Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Category { get; set; }
    public string? EmbeddingReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool HasEmbedding => !string.IsNullOrEmpty(EmbeddingReference);
}

