using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing knowledge base documents (RAG)
/// Handles semantic search and document retrieval for AI context
/// </summary>
public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly ChatDbContext _context;
    private readonly EmbeddingClient _embeddingClient; // For generating embeddings
    private readonly IVectorService _vectorService; // For vector search
    private readonly ICacheService _cacheService;
    private readonly ILogger<KnowledgeRepository> _logger;
    private readonly CacheOptions _cacheOptions;
    private readonly ChatOptions _chatOptions;

    public KnowledgeRepository(
        ChatDbContext context,
        EmbeddingClient embeddingClient,
        IVectorService vectorService,
        ICacheService cacheService,
        ILogger<KnowledgeRepository> logger,
        IOptions<CacheOptions> cacheOptions,
        IOptions<ChatOptions> chatOptions)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _vectorService = vectorService ?? throw new ArgumentNullException(nameof(vectorService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _chatOptions = chatOptions?.Value ?? throw new ArgumentNullException(nameof(chatOptions));
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
            
            // Generate embedding for semantic search (with caching)
            if (!string.IsNullOrWhiteSpace(entity.Content))
            {
                _logger.LogDebug("Generating embedding for document: {Title}", entity.Title);
                
                var embeddingCacheKey = CacheKeyBuilder.EmbeddingFromContent(entity.Content);
                var embedding = await _cacheService.GetOrCreateAsync(
                    embeddingCacheKey,
                    async () =>
                    {
                        var response = await _embeddingClient.GenerateEmbeddingAsync(entity.Content).ConfigureAwait(false);
                        return response.Value.ToFloats().ToArray();
                    },
                    TimeSpan.FromHours(_cacheOptions.EmbeddingExpirationHours)).ConfigureAwait(false);
                
                // Store embedding in Qdrant
                var metadata = new Dictionary<string, string>
                {
                    { "title", entity.Title },
                    { "category", entity.Category ?? "general" },
                    { "source", entity.Source ?? "unknown" }
                };
                
                await _vectorService.StoreEmbeddingAsync(entity.Id, embedding, metadata, ct);
                entity.EmbeddingReference = $"qdrant:{entity.Id}";
                
                _logger.LogDebug("✓ Embedding stored in vector database");
            }
            
            _context.KnowledgeDocuments.Add(entity);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("✅ Added knowledge document {Id}: {Title}", entity.Id, entity.Title);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error adding knowledge document {Title}", entity.Title);
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
            var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(entity.Content);
            var embedding = embeddingResponse.Value.ToFloats().ToArray();
            
            // Update embedding in Qdrant
            var metadata = new Dictionary<string, string>
            {
                { "title", entity.Title },
                { "category", entity.Category ?? "general" },
                { "source", entity.Source ?? "unknown" }
            };
            
            await _vectorService.StoreEmbeddingAsync(entity.Id, embedding, metadata, ct);
            entity.EmbeddingReference = $"qdrant:{entity.Id}";
        }
        
        _context.KnowledgeDocuments.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Updated knowledge document {Id}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _context.KnowledgeDocuments.FindAsync(new object[] { id }, ct);
        if (doc != null)
        {
            // Delete embedding from Qdrant
            await _vectorService.DeleteEmbeddingAsync(id, ct);
            
            _context.KnowledgeDocuments.Remove(doc);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("✅ Deleted knowledge document {Id}", id);
        }
    }

    /// <summary>
    /// RAG CORE: Search knowledge base using semantic similarity (PRODUCTION-READY)
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
            _logger.LogInformation("🔍 Searching knowledge base for: {Query}", query);
            
            // Generate embedding for query
            var embeddingResponse = await _embeddingClient.GenerateEmbeddingAsync(query);
            var queryEmbedding = embeddingResponse.Value.ToFloats().ToArray();
            
            // Search vector database for similar embeddings
            var similarDocIds = await _vectorService.SearchSimilarAsync(
                queryEmbedding, 
                limit: topK * 2, // Get more than needed for filtering
                scoreThreshold: _chatOptions.SearchScoreThreshold, // Use configured threshold
                ct: ct
            );
            
            if (!similarDocIds.Any())
            {
                _logger.LogInformation("❌ No similar documents found above threshold ({Threshold})", _chatOptions.SearchScoreThreshold);
                return Enumerable.Empty<KnowledgeDocument>();
            }
            
            _logger.LogInformation("✅ Vector search found {Count} candidates", similarDocIds.Count);
            
            // Fetch full documents from database
            var docIds = similarDocIds.Select(x => x.DocumentId).ToList();
            var query2 = _context.KnowledgeDocuments
                .AsNoTracking()
                .Where(d => docIds.Contains(d.Id) && d.IsActive);
            
            // Apply category filter if specified
            if (!string.IsNullOrWhiteSpace(category))
            {
                query2 = query2.Where(d => d.Category == category);
            }
            
            var documents = await query2.ToListAsync(ct);
            
            // Sort by similarity score (maintain order from vector search)
            var scoreDict = similarDocIds.ToDictionary(x => x.DocumentId, x => x.Score);
            var sortedDocuments = documents
                .OrderByDescending(d => scoreDict.GetValueOrDefault(d.Id, 0))
                .Take(topK)
                .ToList();
            
            _logger.LogInformation("✓ Returning {Count} relevant documents", sortedDocuments.Count);
            
            // Log similarity scores for debugging
            foreach (var doc in sortedDocuments)
            {
                var score = scoreDict.GetValueOrDefault(doc.Id, 0);
                _logger.LogDebug("  - {Title}: {Score:P0} similarity", doc.Title, score);
            }
            
            return sortedDocuments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge base");
            
            // Fallback to simple text search if vector search fails
            _logger.LogWarning("Falling back to text search");
            return await FallbackTextSearchAsync(query, topK, category, ct);
        }
    }
    
    /// <summary>
    /// Fallback text search if vector search fails
    /// </summary>
    private async Task<IEnumerable<KnowledgeDocument>> FallbackTextSearchAsync(
        string query, 
        int topK, 
        string? category,
        CancellationToken ct)
    {
        var dbQuery = _context.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.IsActive);
        
        if (!string.IsNullOrWhiteSpace(category))
        {
            dbQuery = dbQuery.Where(d => d.Category == category);
        }
        
        var results = await dbQuery
            .Where(d => d.Content.Contains(query) || d.Title.Contains(query))
            .OrderByDescending(d => d.CreatedAt)
            .Take(topK)
            .ToListAsync(ct);
        
        _logger.LogInformation("Fallback search returned {Count} documents", results.Count);
        return results;
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
