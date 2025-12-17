using ChatAI.Application.Configuration;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Resilience;
using ChatAI.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChatAI.Tests.Integration;

/// <summary>
/// Integration tests for vector search
/// Requires Qdrant to be running on localhost:6333
/// </summary>
[Trait("Category", "Integration")]
public class VectorSearchIntegrationTests
{
    private readonly IVectorService _vectorService;
    private readonly ILogger<QdrantVectorService> _logger;
    private readonly QdrantOptions _options;
    private readonly ResiliencePolicies _resiliencePolicies;

    public VectorSearchIntegrationTests()
    {
        // Use test collection to avoid conflicts
        _options = new QdrantOptions
        {
            Endpoint = "http://localhost:6333",
            CollectionName = $"test_{Guid.NewGuid():N}",
            VectorSize = 1536
        };

        var resilienceOptions = new ResilienceOptions
        {
            Enabled = false // Disable resilience for tests
        };

        _logger = NullLogger<QdrantVectorService>.Instance;
        var resilienceLogger = NullLogger<ResiliencePolicies>.Instance;
        _resiliencePolicies = new ResiliencePolicies(Options.Create(resilienceOptions), resilienceLogger);
        _vectorService = new QdrantVectorService(_logger, Options.Create(_options), _resiliencePolicies);
    }

    [Fact(Skip = "Requires Qdrant running")]
    public async Task InitializeAsync_ShouldCreateCollection()
    {
        // Act
        await _vectorService.InitializeAsync();

        // Assert
        var stats = await _vectorService.GetStatsAsync();
        stats.TotalVectors.Should().Be(0);
    }

    [Fact(Skip = "Requires Qdrant running")]
    public async Task StoreAndSearch_ShouldFindSimilarVectors()
    {
        // Arrange
        await _vectorService.InitializeAsync();

        var doc1Id = Guid.NewGuid();
        var doc2Id = Guid.NewGuid();

        // Create two similar vectors
        var embedding1 = CreateTestEmbedding(1.0f, 0.5f);
        var embedding2 = CreateTestEmbedding(0.9f, 0.6f); // Similar to embedding1

        var metadata1 = new Dictionary<string, string>
        {
            ["document_id"] = doc1Id.ToString(),
            ["title"] = "Document 1"
        };

        var metadata2 = new Dictionary<string, string>
        {
            ["document_id"] = doc2Id.ToString(),
            ["title"] = "Document 2"
        };

        // Act - Store embeddings
        await _vectorService.StoreEmbeddingAsync(doc1Id, embedding1, metadata1);
        await _vectorService.StoreEmbeddingAsync(doc2Id, embedding2, metadata2);

        // Act - Search
        var results = await _vectorService.SearchSimilarAsync(embedding1, limit: 5, scoreThreshold: 0.5);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.DocumentId == doc1Id);
        results.First().DocumentId.Should().Be(doc1Id); // Exact match should be first
        results.First().Score.Should().BeGreaterThan(0.99);

        // Cleanup
        await _vectorService.DeleteEmbeddingAsync(doc1Id);
        await _vectorService.DeleteEmbeddingAsync(doc2Id);
    }

    [Fact(Skip = "Requires Qdrant running")]
    public async Task DeleteEmbedding_ShouldRemoveFromIndex()
    {
        // Arrange
        await _vectorService.InitializeAsync();
        var docId = Guid.NewGuid();
        var embedding = CreateTestEmbedding(1.0f, 0.5f);
        var metadata = new Dictionary<string, string> { ["document_id"] = docId.ToString() };

        await _vectorService.StoreEmbeddingAsync(docId, embedding, metadata);

        // Act
        await _vectorService.DeleteEmbeddingAsync(docId);

        // Assert
        var results = await _vectorService.SearchSimilarAsync(embedding, limit: 10);
        results.Should().NotContain(r => r.DocumentId == docId);
    }

    private static float[] CreateTestEmbedding(float baseValue, float variance)
    {
        var embedding = new float[1536];
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = baseValue + ((float)random.NextDouble() - 0.5f) * variance;
        }

        return embedding;
    }
}
