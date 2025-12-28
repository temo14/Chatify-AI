namespace ChatAI.Domain.Entities;

/// <summary>
/// Knowledge base documents for Chatify AI
/// These are the documents that AI can reference (RAG)
/// Managed via control panel (future feature)
/// Examples: FAQs, documentation, company info, product details
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Tenant (customer) that owns this document
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Document title or heading
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Full document content (will be chunked for embeddings)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Source of the document (URL, file name, manual entry, etc.)
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Category for organization (e.g., "FAQ", "Product", "Policy", "Technical")
    /// Used in future control panel for filtering
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Reference to vector embedding in Qdrant (for semantic search)
    /// Format: "qdrant:{documentId}" or "sql:{documentId}"
    /// </summary>
    public string? EmbeddingReference { get; set; }
    
    /// <summary>
    /// Actual embedding data for SQL storage mode (JSON serialized)
    /// Only used when VectorStorageMode = "InMemory" (SQL-based)
    /// NULL when using Qdrant (embedding stored externally)
    /// </summary>
    public string? EmbeddingData { get; set; }
    
    /// <summary>
    /// When this document was added
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last time this document was updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON (tags, author, version, language, etc.)
    /// Flexible for future control panel features
    /// </summary>
    public string? MetadataJson { get; set; }
    
    /// <summary>
    /// Whether this document is active and should be included in AI searches
    /// Control panel can toggle this
    /// </summary>
    public bool IsActive { get; set; } = true;
}
