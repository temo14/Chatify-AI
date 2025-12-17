using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Response;

using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Knowledge.SearchKnowledge;

public class SearchKnowledgeQueryHandler : IRequestHandler<SearchKnowledgeQuery, IEnumerable<KnowledgeDocumentResponse>>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<SearchKnowledgeQueryHandler> _logger;

    public SearchKnowledgeQueryHandler(
        IKnowledgeRepository repository,
        ILogger<SearchKnowledgeQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<KnowledgeDocumentResponse>> Handle(SearchKnowledgeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Searching knowledge base: query='{Query}', limit={Limit}", request.Query, request.Limit);
        
        var results = await _repository.SearchAsync(request.Query, request.Limit);
        
        // Filter by category if specified
        if (!string.IsNullOrEmpty(request.Category))
        {
            results = results.Where(r => r.Category == request.Category);
        }

        var responses = results.Select(r => KnowledgeDocumentResponse.ToSummary(r, 300)).ToList();
        
        _logger.LogInformation("✅ Found {Count} relevant documents", responses.Count);
        
        return responses;
    }
}
