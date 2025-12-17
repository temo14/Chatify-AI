using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Knowledge.GetDocument;

/// <summary>
/// Query to get a specific knowledge document by ID
/// </summary>
public class GetKnowledgeDocumentQuery : IRequest<KnowledgeDocumentResponse?>
{
    public Guid Id { get; set; }
}
