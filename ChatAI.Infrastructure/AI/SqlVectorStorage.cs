using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// SQL-based vector storage using cosine similarity
/// Best for < 100 documents per tenant (fast, cheap, simple)
/// </summary>
public class SqlVectorStorage : IVectorStorage
{
    private readonly ChatDbContext _context;
    private readonly Guid _tenantId;
    private readonly ILogger<SqlVectorStorage> _logger;

    public SqlVectorStorage(
        ChatDbContext context,
        Guid tenantId,
        ILogger<SqlVectorStorage> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantId = tenantId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        // No initialization needed for SQL storage (table already exists)
        _logger.LogInformation("SQL vector storage initialized for tenant {TenantId}", _tenantId);
        return Task.CompletedTask;
    }

    public async Task StoreEmbeddingAsync(
        Guid documentId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        var doc = await _context.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == _tenantId, ct);

        if (doc == null)
        {
            throw new InvalidOperationException($"Document {documentId} not found for tenant {_tenantId}");
        }

        // Store embedding as JSON in EmbeddingReference field
        var embeddingJson = JsonSerializer.Serialize(new
        {
            vector = embedding,
            metadata = metadata,
            storage = "sql"
        });

        doc.EmbeddingReference = embeddingJson;
        doc.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Stored embedding for document {DocumentId} (tenant {TenantId}) in SQL", 
            documentId, _tenantId);
    }

    public async Task<IEnumerable<(Guid DocumentId, double Similarity)>> SearchSimilarAsync(
        float[] queryEmbedding,
        int limit = 5,
        double scoreThreshold = 0.0,
        CancellationToken ct = default)
    {
        // Load all documents with embeddings for this tenant
        var documents = await _context.KnowledgeDocuments
            .Where(d => d.TenantId == _tenantId && d.IsActive && d.EmbeddingReference != null)
            .Select(d => new { d.Id, d.EmbeddingReference })
            .ToListAsync(ct);

        if (!documents.Any())
        {
            return Enumerable.Empty<(Guid, double)>();
        }

        // Calculate cosine similarity in-memory (fast for < 1000 vectors)
        var similarities = new List<(Guid DocumentId, double Similarity)>();

        foreach (var doc in documents)
        {
            try
            {
                var embeddingData = JsonSerializer.Deserialize<EmbeddingData>(doc.EmbeddingReference!);
                if (embeddingData?.Vector != null)
                {
                    var similarity = CosineSimilarity(queryEmbedding, embeddingData.Vector);
                    if (similarity >= scoreThreshold)
                    {
                        similarities.Add((doc.Id, similarity));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse embedding for document {DocumentId}", doc.Id);
            }
        }

        return similarities
            .OrderByDescending(s => s.Similarity)
            .Take(limit)
            .ToList();
    }

    public async Task DeleteEmbeddingAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _context.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == _tenantId, ct);

        if (doc != null)
        {
            doc.EmbeddingReference = null;
            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug("Deleted embedding for document {DocumentId} (tenant {TenantId})", 
                documentId, _tenantId);
        }
    }

    public async Task<VectorStorageStats> GetStatsAsync(CancellationToken ct = default)
    {
        var count = await _context.KnowledgeDocuments
            .Where(d => d.TenantId == _tenantId && d.EmbeddingReference != null)
            .CountAsync(ct);

        return new VectorStorageStats
        {
            TotalVectors = count,
            StorageMode = "SQL",
            StorageSizeBytes = count * 6144 // Approximate: 1536 floats * 4 bytes
        };
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same dimension");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private class EmbeddingData
    {
        public float[]? Vector { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? Storage { get; set; }
    }
}
