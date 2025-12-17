using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Interface for vector database operations (embeddings storage and search)
/// </summary>
public interface IVectorService
{
    /// <summary>
    /// Initialize the vector database collection
    /// Creates collection if it doesn't exist
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Store a document's embedding in the vector database
    /// </summary>
    /// <param name="documentId">Unique ID of the document</param>
    /// <param name="embedding">Vector embedding (1536 dimensions)</param>
    /// <param name="metadata">Metadata to store with the vector (title, category, etc.)</param>
    Task StoreEmbeddingAsync(Guid documentId, float[] embedding, Dictionary<string, string> metadata, CancellationToken ct = default);

    /// <summary>
    /// Search for similar documents using vector similarity
    /// </summary>
    /// <param name="queryEmbedding">Embedding of the search query</param>
    /// <param name="limit">Number of results to return</param>
    /// <param name="scoreThreshold">Minimum similarity score (0-1, optional)</param>
    /// <returns>List of document IDs with similarity scores</returns>
    Task<List<(Guid DocumentId, double Score)>> SearchSimilarAsync(
        float[] queryEmbedding, 
        int limit = 5, 
        double? scoreThreshold = null,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a document's embedding from vector database
    /// </summary>
    Task DeleteEmbeddingAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Delete all embeddings from the collection
    /// </summary>
    Task ClearAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Get collection statistics (total vectors, size, etc.)
    /// </summary>
    Task<(int TotalVectors, long MemorySize)> GetStatsAsync(CancellationToken ct = default);
}
