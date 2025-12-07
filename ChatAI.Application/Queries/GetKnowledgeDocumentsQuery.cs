using ChatAI.Application.Models.Response;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get all knowledge documents with optional filtering
/// </summary>
public class GetKnowledgeDocumentsQuery : IRequest<IEnumerable<KnowledgeDocumentResponse>>
{
    public bool OnlyActive { get; set; }
    public string? Category { get; set; }
}
