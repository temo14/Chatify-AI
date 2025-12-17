using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Models.Response;

/// <summary>
/// Response model for knowledge documents
/// </summary>
public class KnowledgeDocumentResponse
{
    public Guid Id { get; set; }
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

    public static KnowledgeDocumentResponse FromEntity(KnowledgeDocument entity)
    {
        return new KnowledgeDocumentResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            Source = entity.Source,
            Category = entity.Category,
            EmbeddingReference = entity.EmbeddingReference,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive
        };
    }

    public static KnowledgeDocumentResponse ToSummary(KnowledgeDocument entity, int contentPreviewLength = 200)
    {
        var response = FromEntity(entity);
        
        if (response.Content.Length > contentPreviewLength)
        {
            response.Content = response.Content.Substring(0, contentPreviewLength) + "...";
        }
        
        return response;
    }
}
