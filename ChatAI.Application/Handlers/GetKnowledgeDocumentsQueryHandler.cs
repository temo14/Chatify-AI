using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Response;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

public class GetKnowledgeDocumentsQueryHandler : IRequestHandler<GetKnowledgeDocumentsQuery, IEnumerable<KnowledgeDocumentResponse>>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<GetKnowledgeDocumentsQueryHandler> _logger;

    public GetKnowledgeDocumentsQueryHandler(
        IKnowledgeRepository repository,
        ILogger<GetKnowledgeDocumentsQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<KnowledgeDocumentResponse>> Handle(GetKnowledgeDocumentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving knowledge documents - OnlyActive: {OnlyActive}, Category: {Category}", 
            request.OnlyActive, request.Category ?? "all");

        IEnumerable<KnowledgeDocument> documents;

        if (!string.IsNullOrEmpty(request.Category))
        {
            documents = await _repository.GetByCategoryAsync(request.Category);
        }
        else if (request.OnlyActive)
        {
            documents = await _repository.GetActiveDocumentsAsync();
        }
        else
        {
            documents = await _repository.GetAllAsync();
        }

        var results = documents.Select(d => KnowledgeDocumentResponse.ToSummary(d, 200)).ToList();
        
        _logger.LogInformation("Retrieved {Count} knowledge documents", results.Count);
        
        return results;
    }
}
