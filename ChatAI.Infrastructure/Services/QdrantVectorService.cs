using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Qdrant implementation of vector database service
/// Handles embedding storage and semantic search for RAG
/// </summary>
public class QdrantVectorService : IVectorService
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorService> _logger;
    private readonly QdrantOptions _options;
    private readonly ResiliencePolicies _resiliencePolicies;

    public QdrantVectorService(
        ILogger<QdrantVectorService> logger,
        IOptions<QdrantOptions> options,
        ResiliencePolicies resiliencePolicies)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _resiliencePolicies = resiliencePolicies ?? throw new ArgumentNullException(nameof(resiliencePolicies));

        // Parse endpoint to extract host
        var uri = new Uri(_options.Endpoint);
        var host = uri.Host;
        
        // Qdrant uses gRPC port 6334 by default (not the REST API port 6333)
        var grpcPort = 6334;
        var isHttps = uri.Scheme == "https";

        // Create Qdrant client with gRPC settings
        _client = new QdrantClient(host, grpcPort, isHttps);
        
        _logger.LogInformation("Qdrant client initialized for {Host}:{Port} (gRPC, HTTPS: {IsHttps})", 
            host, grpcPort, isHttps);
    }

    /// <summary>
    /// Convert GUID to numeric ID for Qdrant (uses first 8 bytes as ulong)
    /// </summary>
    private static ulong GuidToNumericId(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return BitConverter.ToUInt64(bytes, 0);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var retryPolicy = _resiliencePolicies.GetRetryPolicy<bool>("Qdrant-Initialize");
        
        await retryPolicy.ExecuteAsync(async token =>
        {
            try
            {
                _logger.LogInformation("Initializing Qdrant collection: {CollectionName}", _options.CollectionName);

                var collections = await _client.ListCollectionsAsync(cancellationToken: token);
                var collectionExists = collections.Contains(_options.CollectionName);

                if (!collectionExists)
                {
                    _logger.LogInformation("Creating new collection with vector size {VectorSize}", _options.VectorSize);

                    await _client.CreateCollectionAsync(
                        collectionName: _options.CollectionName,
                        vectorsConfig: new VectorParams
                        {
                            Size = _options.VectorSize,
                            Distance = Distance.Cosine
                        },
                        cancellationToken: token
                    );

                    _logger.LogInformation("✅ Collection created successfully");
                }
                else
                {
                    _logger.LogInformation("✅ Collection already exists");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Qdrant");
                throw;
            }
        }, ct);
    }

    public async Task StoreEmbeddingAsync(
        Guid documentId, 
        float[] embedding, 
        Dictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var retryPolicy = _resiliencePolicies.GetRetryPolicy<bool>("Qdrant-StoreEmbedding");
        
        await retryPolicy.ExecuteAsync(async token =>
        {
            try
            {
                var payload = new Dictionary<string, Value>();
                // Store original GUID in payload for retrieval
                payload["document_id"] = documentId.ToString();
                foreach (var kvp in metadata)
                {
                    payload[kvp.Key] = kvp.Value;
                }

                var point = new PointStruct
                {
                    Id = new PointId { Num = GuidToNumericId(documentId) },
                    Vectors = embedding,
                    Payload = { payload }
                };

                await _client.UpsertAsync(
                    collectionName: _options.CollectionName,
                    points: new[] { point },
                    cancellationToken: token
                );

                _logger.LogDebug("✅ Stored embedding for {DocumentId}", documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to store embedding for {DocumentId}", documentId);
                throw;
            }
        }, ct);
    }

    public async Task<List<(Guid DocumentId, double Score)>> SearchSimilarAsync(
        float[] queryEmbedding,
        int limit = 5,
        double? scoreThreshold = null,
        CancellationToken ct = default)
    {
        var retryPolicy = _resiliencePolicies.GetRetryPolicy<List<(Guid, double)>>("Qdrant-SearchSimilar");
        
        return await retryPolicy.ExecuteAsync(async token =>
        {
            try
            {
                var searchResults = await _client.SearchAsync(
                    collectionName: _options.CollectionName,
                    vector: queryEmbedding,
                    limit: (ulong)limit,
                    scoreThreshold: scoreThreshold.HasValue ? (float)scoreThreshold.Value : null,
                    payloadSelector: true, // Include payload to get document_id
                    cancellationToken: token
                );

                var results = searchResults
                    .Select(r => {
                        // Extract original GUID from payload
                        var guidStr = r.Payload["document_id"].StringValue;
                        return (
                            DocumentId: Guid.Parse(guidStr),
                            Score: (double)r.Score
                        );
                    })
                    .ToList();

                _logger.LogInformation("✅ Found {Count} similar documents", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to search similar documents");
                throw;
            }
        }, ct);
    }

    public async Task DeleteEmbeddingAsync(Guid documentId, CancellationToken ct = default)
    {
        var retryPolicy = _resiliencePolicies.GetRetryPolicy<bool>("Qdrant-DeleteEmbedding");
        
        await retryPolicy.ExecuteAsync(async token =>
        {
            try
            {
                var numericId = GuidToNumericId(documentId);
                
                await _client.DeleteAsync(
                    collectionName: _options.CollectionName,
                    id: numericId,
                    wait: true,
                    cancellationToken: token
                );

                _logger.LogDebug("✅ Deleted embedding for {DocumentId}", documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete embedding for {DocumentId}", documentId);
                throw;
            }
        }, ct);
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteCollectionAsync(_options.CollectionName, cancellationToken: ct);
            await InitializeAsync(ct);
            _logger.LogInformation("✓ All embeddings cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear embeddings");
            throw;
        }
    }

    public async Task<(int TotalVectors, long MemorySize)> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(_options.CollectionName, cancellationToken: ct);
            var totalVectors = (int)info.VectorsCount;
            var memorySize = (long)info.PointsCount * _options.VectorSize * sizeof(float);
            return (totalVectors, memorySize);
        }
        catch
        {
            return (0, 0);
        }
    }
}
