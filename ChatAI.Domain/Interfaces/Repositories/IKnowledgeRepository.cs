using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository for managing the knowledge base (RAG)
/// </summary>
public interface IKnowledgeRepository : IRepository<KnowledgeDocument>
{
    /// <summary>
    /// RAG: Search knowledge base using semantic similarity
    /// This is the core RAG functionality - finds relevant documents for a query
    /// </summary>
    /// <param name="query">User's question or search query</param>
    /// <param name="topK">Number of most relevant documents to return</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of relevant knowledge documents</returns>
    Task<IEnumerable<KnowledgeDocument>> SearchAsync(
        string query, 
        int topK = 5, 
        string? category = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get documents by source (e.g., all docs from a specific file or URL)
    /// </summary>
    Task<IEnumerable<KnowledgeDocument>> GetBySourceAsync(string source, CancellationToken ct = default);
    
    /// <summary>
    /// Get documents by category
    /// </summary>
    Task<IEnumerable<KnowledgeDocument>> GetByCategoryAsync(string category, CancellationToken ct = default);
    
    /// <summary>
    /// Get all active documents
    /// </summary>
    Task<IEnumerable<KnowledgeDocument>> GetActiveDocumentsAsync(CancellationToken ct = default);
}
