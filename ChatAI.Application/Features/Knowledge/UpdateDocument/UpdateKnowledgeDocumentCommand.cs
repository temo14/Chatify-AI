using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Knowledge.UpdateDocument;

/// <summary>
/// Command to update an existing knowledge document (regenerates embeddings)
/// </summary>
public class UpdateKnowledgeDocumentCommand : IRequest<KnowledgeDocumentResponse>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Category { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; } = true;
}
