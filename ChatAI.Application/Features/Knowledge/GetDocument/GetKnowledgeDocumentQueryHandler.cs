using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Response;

using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Knowledge.GetDocument;

public class GetKnowledgeDocumentQueryHandler : IRequestHandler<GetKnowledgeDocumentQuery, KnowledgeDocumentResponse?>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<GetKnowledgeDocumentQueryHandler> _logger;

    public GetKnowledgeDocumentQueryHandler(
        IKnowledgeRepository repository,
        ILogger<GetKnowledgeDocumentQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeDocumentResponse?> Handle(GetKnowledgeDocumentQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving knowledge document {DocumentId}", request.Id);

        var document = await _repository.GetByIdAsync(request.Id);
        
        if (document == null)
        {
            _logger.LogWarning("Knowledge document {DocumentId} not found", request.Id);
            return null;
        }

        return KnowledgeDocumentResponse.FromEntity(document);
    }
}
