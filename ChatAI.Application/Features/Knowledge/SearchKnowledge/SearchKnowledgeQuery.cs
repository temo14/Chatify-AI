using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Knowledge.SearchKnowledge;

/// <summary>
/// Query to search knowledge documents using semantic similarity
/// </summary>
public class SearchKnowledgeQuery : IRequest<IEnumerable<KnowledgeDocumentResponse>>
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 5;
    public string? Category { get; set; }
}
