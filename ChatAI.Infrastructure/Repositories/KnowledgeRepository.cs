using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing knowledge base documents (RAG)
/// Handles semantic search and document retrieval for AI context
/// </summary>
public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly ChatDbContext _context;
    private readonly IAIClient _aiClient; // For generating embeddings
    private readonly ILogger<KnowledgeRepository> _logger;

    public KnowledgeRepository(
        ChatDbContext context,
        IAIClient aiClient,
        ILogger<KnowledgeRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.KnowledgeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.KnowledgeDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<KnowledgeDocument> AddAsync(KnowledgeDocument entity, CancellationToken ct = default)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            
            // Generate embedding for semantic search
            if (!string.IsNullOrWhiteSpace(entity.Content))
            {
                _logger.LogDebug("Generating embedding for document: {Title}", entity.Title);
                var embedding = await _aiClient.GenerateEmbeddingAsync(entity.Content, ct);
                entity.EmbeddingReference = $"emb_{entity.Id}";
                
                // TODO: Store embedding vector in vector database (Qdrant, Pinecone, Azure AI Search)
                // For now, we just store a reference
                _logger.LogDebug("Embedding generated (would store in vector DB)");
            }
            
            _context.KnowledgeDocuments.Add(entity);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Added knowledge document {Id}: {Title}", entity.Id, entity.Title);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding knowledge document {Title}", entity.Title);
            throw;
        }
    }

    public async Task UpdateAsync(KnowledgeDocument entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        // If content changed, regenerate embedding
        var existing = await _context.KnowledgeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == entity.Id, ct);
        
        if (existing != null && existing.Content != entity.Content)
        {
            _logger.LogDebug("Content changed, regenerating embedding for {Title}", entity.Title);
            var embedding = await _aiClient.GenerateEmbeddingAsync(entity.Content, ct);
            entity.EmbeddingReference = $"emb_{entity.Id}";
            // TODO: Update vector in vector DB
        }
        
        _context.KnowledgeDocuments.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Updated knowledge document {Id}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _context.KnowledgeDocuments.FindAsync(new object[] { id }, ct);
        if (doc != null)
        {
            // TODO: Delete embedding from vector DB
            _context.KnowledgeDocuments.Remove(doc);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted knowledge document {Id}", id);
        }
    }

    /// <summary>
    /// RAG CORE: Search knowledge base using semantic similarity
    /// This is where the magic happens - finding relevant context for AI
    /// </summary>
    public async Task<IEnumerable<KnowledgeDocument>> SearchAsync(
        string query, 
        int topK = 5, 
        string? category = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching knowledge base for: {Query}", query);
            
            // Generate embedding for the query
            var queryEmbedding = await _aiClient.GenerateEmbeddingAsync(query, ct);
            
            // TODO: Query vector database for similar embeddings
            // For now, use simple text search as fallback
            _logger.LogWarning("Vector search not implemented yet, using text fallback");
            
            var dbQuery = _context.KnowledgeDocuments
                .AsNoTracking()
                .Where(d => d.IsActive);
            
            // Filter by category if specified
            if (!string.IsNullOrWhiteSpace(category))
            {
                dbQuery = dbQuery.Where(d => d.Category == category);
            }
            
            // Simple text contains search (replace with vector similarity later)
            var results = await dbQuery
                .Where(d => d.Content.Contains(query) || d.Title.Contains(query))
                .OrderByDescending(d => d.CreatedAt)
                .Take(topK)
                .ToListAsync(ct);
            
            _logger.LogInformation("Found {Count} relevant documents", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge base");
            return Enumerable.Empty<KnowledgeDocument>();
        }
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetBySourceAsync(string source, CancellationToken ct = default)
    {
        return await _context.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.Source == source && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await _context.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.Category == category && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetActiveDocumentsAsync(CancellationToken ct = default)
    {
        return await _context.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }
}
