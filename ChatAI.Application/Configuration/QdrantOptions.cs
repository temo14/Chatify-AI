namespace ChatAI.Application.Configuration;

/// <summary>
/// Configuration for Qdrant vector database
/// </summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>
    /// Qdrant server endpoint (e.g., http://localhost:6333)
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:6333";

    /// <summary>
    /// Name of the collection to store knowledge embeddings
    /// </summary>
    public string CollectionName { get; set; } = "knowledge-base";

    /// <summary>
    /// Size of embedding vectors (1536 for text-embedding-3-small, 3072 for text-embedding-3-large)
    /// </summary>
    public uint VectorSize { get; set; } = 1536;

    /// <summary>
    /// API key for Qdrant (if using Qdrant Cloud)
    /// </summary>
    public string? ApiKey { get; set; }
}
