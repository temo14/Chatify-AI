using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Qdrant.Client;

namespace ChatAI.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Qdrant vector database connectivity
/// </summary>
public class QdrantHealthCheck : IHealthCheck
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantHealthCheck> _logger;
    private readonly string _collectionName;

    public QdrantHealthCheck(
        QdrantClient client, 
        ILogger<QdrantHealthCheck> logger,
        string collectionName = "chatai_knowledge")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collectionName = collectionName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if collection exists
            var collectionExists = await _client.CollectionExistsAsync(_collectionName, cancellationToken);
            
            if (!collectionExists)
            {
                _logger.LogWarning("Qdrant collection '{CollectionName}' does not exist", _collectionName);
                return HealthCheckResult.Degraded(
                    $"Qdrant is accessible but collection '{_collectionName}' does not exist");
            }

            // Get collection info to verify connectivity
            var collectionInfo = await _client.GetCollectionInfoAsync(_collectionName, cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                { "collection", _collectionName },
                { "vectorCount", collectionInfo.VectorsCount },
                { "pointsCount", collectionInfo.PointsCount },
                { "status", collectionInfo.Status.ToString() },
                { "indexedVectorsCount", collectionInfo.IndexedVectorsCount }
            };

            _logger.LogDebug("Qdrant health check passed: {PointsCount} points in collection", 
                collectionInfo.PointsCount);

            return HealthCheckResult.Healthy(
                $"Qdrant is healthy with {collectionInfo.PointsCount} points", 
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant health check failed");
            return HealthCheckResult.Unhealthy(
                "Qdrant is not accessible", 
                ex);
        }
    }
}
