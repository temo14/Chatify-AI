using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a piece of long-term user memory for personalization
/// </summary>
public class UserMemory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Summary or full content
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Category { get; set; } = string.Empty; // e.g., "preference", "fact", "context"
    public MemoryImportance Importance { get; set; } = MemoryImportance.Medium;
    
    // Optional: For RAG/semantic search
    public string? EmbeddingReference { get; set; }
    public double? RelevanceScore { get; set; }
}
