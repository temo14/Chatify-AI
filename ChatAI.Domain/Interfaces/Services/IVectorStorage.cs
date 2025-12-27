using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Interface for vector storage operations
/// Implementations: SqlVectorStorage, QdrantVectorStorage
/// </summary>
public interface IVectorStorage
{
    /// <summary>
    /// Initialize the vector storage (create collection/table if needed)
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Store a document embedding
    /// </summary>
    Task StoreEmbeddingAsync(
        Guid documentId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search for similar documents using vector similarity
    /// </summary>
    Task<IEnumerable<(Guid DocumentId, double Similarity)>> SearchSimilarAsync(
        float[] queryEmbedding,
        int limit = 5,
        double scoreThreshold = 0.0,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a document embedding
    /// </summary>
    Task DeleteEmbeddingAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get storage statistics
    /// </summary>
    Task<VectorStorageStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about vector storage
/// </summary>
public class VectorStorageStats
{
    public int TotalVectors { get; set; }
    public string StorageMode { get; set; } = string.Empty;
    public long StorageSizeBytes { get; set; }
}
