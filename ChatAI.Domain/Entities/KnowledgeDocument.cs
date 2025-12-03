namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents your "base knowledge" - documents that AI can always reference.
/// This is the foundation for RAG (Retrieval-Augmented Generation).
/// Examples: Company policies, product docs, FAQs, training materials, etc.
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Document title or heading
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Full document content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Source of the document (URL, file path, etc.)
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Category or type of knowledge (e.g., "policy", "faq", "technical", "product")
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Reference to vector embedding (for semantic search)
    /// This could be an ID in a vector database like Qdrant, Pinecone, etc.
    /// </summary>
    public string? EmbeddingReference { get; set; }
    
    /// <summary>
    /// When this document was added to the knowledge base
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last time this document was updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON (tags, author, version, etc.)
    /// </summary>
    public string? MetadataJson { get; set; }
    
    /// <summary>
    /// Whether this document is active and should be included in searches
    /// </summary>
    public bool IsActive { get; set; } = true;
}
