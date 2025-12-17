
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Knowledge.LoadDocumentsToQdrant;

public class LoadDocumentsToQdrantCommandHandler : IRequestHandler<LoadDocumentsToQdrantCommand, LoadToQdrantResponse>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<LoadDocumentsToQdrantCommandHandler> _logger;

    public LoadDocumentsToQdrantCommandHandler(
        IKnowledgeRepository repository,
        ILogger<LoadDocumentsToQdrantCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoadToQdrantResponse> Handle(LoadDocumentsToQdrantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting batch load of knowledge documents to Qdrant");
        
        var documents = await _repository.GetAllAsync();
        var response = new LoadToQdrantResponse
        {
            TotalDocuments = documents.Count()
        };

        foreach (var doc in documents)
        {
            try
            {
                // Skip if already vectorized
                if (!string.IsNullOrEmpty(doc.EmbeddingReference))
                {
                    response.SkippedCount++;
                    _logger.LogDebug("Skipping document {DocumentId} - already has embedding", doc.Id);
                    continue;
                }

                // UpdateAsync will generate embedding and store in Qdrant
                await _repository.UpdateAsync(doc);
                response.LoadedCount++;
                
                _logger.LogInformation("Loaded document {DocumentId}: {Title}", doc.Id, doc.Title);
            }
            catch (Exception ex)
            {
                response.ErrorCount++;
                response.Errors.Add($"Document {doc.Id} ({doc.Title}): {ex.Message}");
                _logger.LogError(ex, "Failed to load document {DocumentId}", doc.Id);
            }
        }

        _logger.LogInformation("Batch load complete - {Message}", response.Message);

        return response;
    }
}
