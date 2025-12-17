
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Response;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Knowledge.AddDocument;

public class AddKnowledgeDocumentCommandHandler : IRequestHandler<AddKnowledgeDocumentCommand, KnowledgeDocumentResponse>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<AddKnowledgeDocumentCommandHandler> _logger;

    public AddKnowledgeDocumentCommandHandler(
        IKnowledgeRepository repository,
        ILogger<AddKnowledgeDocumentCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeDocumentResponse> Handle(AddKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding new knowledge document: {Title}", request.Title);

        var document = new KnowledgeDocument
        {
            Title = request.Title,
            Content = request.Content,
            Source = request.Source,
            Category = request.Category,
            MetadataJson = request.MetadataJson,
            IsActive = request.IsActive
        };

        // Repository automatically generates embeddings and stores in Qdrant
        var created = await _repository.AddAsync(document);

        _logger.LogInformation(
            "✅ Created knowledge document {DocumentId}: {Title} with embedding reference {EmbeddingRef}",
            created.Id, created.Title, created.EmbeddingReference);

        return KnowledgeDocumentResponse.FromEntity(created);
    }
}
