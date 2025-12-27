using ChatAI.Application.Configuration;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Qdrant-based vector storage using Qdrant Cloud or self-hosted Qdrant
/// Best for > 100 documents per tenant (fast, scalable, advanced filtering)
/// </summary>
public class QdrantVectorStorage : IVectorStorage
{
    private readonly QdrantClient _client;
    private readonly Guid _tenantId;
    private readonly string _collectionName;
    private readonly ILogger<QdrantVectorStorage> _logger;
    private readonly QdrantOptions _options;

    public QdrantVectorStorage(
        Guid tenantId,
        string collectionName,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStorage> logger)
    {
        _tenantId = tenantId;
        _collectionName = collectionName;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _client = new QdrantClient(_options.Endpoint);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync(cancellationToken: ct);
            var exists = collections.Any(c => c == _collectionName); // Collections is IEnumerable<string>

            if (!exists)
            {
                // Create collection with vector configuration
                await _client.CreateCollectionAsync(
                    collectionName: _collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = (ulong)_options.VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: ct);

                _logger.LogInformation("Created Qdrant collection {CollectionName} for tenant {TenantId}", 
                    _collectionName, _tenantId);
            }
            else
            {
                _logger.LogDebug("Qdrant collection {CollectionName} already exists", _collectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Qdrant collection {CollectionName}", _collectionName);
            throw;
        }
    }

    public async Task StoreEmbeddingAsync(
        Guid documentId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        try
        {
            var pointId = new PointId { Uuid = documentId.ToString() };
            
            var payload = new Dictionary<string, Value>
            {
                ["tenant_id"] = _tenantId.ToString(),
                ["document_id"] = documentId.ToString()
            };

            // Add metadata to payload
            foreach (var kvp in metadata)
            {
                payload[kvp.Key] = ConvertToValue(kvp.Value);
            }

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = embedding,
                Payload = { payload }
            };

            await _client.UpsertAsync(
                collectionName: _collectionName,
                points: new[] { point },
                cancellationToken: ct);

            _logger.LogDebug("Stored embedding for document {DocumentId} in Qdrant collection {CollectionName}", 
                documentId, _collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store embedding in Qdrant for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<IEnumerable<(Guid DocumentId, double Similarity)>> SearchSimilarAsync(
        float[] queryEmbedding,
        int limit = 5,
        double scoreThreshold = 0.0,
        CancellationToken ct = default)
    {
        try
        {
            // Search with tenant filter
            var searchResult = await _client.SearchAsync(
                collectionName: _collectionName,
                vector: queryEmbedding,
                filter: new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "tenant_id",
                                Match = new Match
                                {
                                    Keyword = _tenantId.ToString()
                                }
                            }
                        }
                    }
                },
                limit: (ulong)limit,
                scoreThreshold: (float)scoreThreshold,
                cancellationToken: ct);

            return searchResult.Select(r => (
                DocumentId: Guid.Parse(r.Id.Uuid),
                Similarity: (double)r.Score
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search in Qdrant collection {CollectionName}", _collectionName);
            throw;
        }
    }

    public async Task DeleteEmbeddingAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            var pointIds = new ulong[] { (ulong)documentId.GetHashCode() }; // Convert Guid to ulong

            await _client.DeleteAsync(
                collectionName: _collectionName,
                ids: pointIds,
                cancellationToken: ct);

            _logger.LogDebug("Deleted embedding for document {DocumentId} from Qdrant", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete embedding from Qdrant for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<VectorStorageStats> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(_collectionName, cancellationToken: ct);
            
            // Count vectors for this tenant
            var count = await _client.CountAsync(
                collectionName: _collectionName,
                filter: new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "tenant_id",
                                Match = new Match
                                {
                                    Keyword = _tenantId.ToString()
                                }
                            }
                        }
                    }
                },
                exact: true,
                cancellationToken: ct);

            return new VectorStorageStats
            {
                TotalVectors = (int)count,
                StorageMode = "Qdrant",
                StorageSizeBytes = (long)(count * _options.VectorSize * 4)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats from Qdrant");
            return new VectorStorageStats
            {
                TotalVectors = 0,
                StorageMode = "Qdrant",
                StorageSizeBytes = 0
            };
        }
    }

    private static Value ConvertToValue(object obj)
    {
        return obj switch
        {
            string s => new Value { StringValue = s },
            int i => new Value { IntegerValue = i },
            long l => new Value { IntegerValue = l },
            double d => new Value { DoubleValue = d },
            float f => new Value { DoubleValue = f },
            bool b => new Value { BoolValue = b },
            _ => new Value { StringValue = obj.ToString() ?? string.Empty }
        };
    }
}
