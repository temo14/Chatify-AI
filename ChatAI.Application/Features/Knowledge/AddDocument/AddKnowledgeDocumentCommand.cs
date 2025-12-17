using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Knowledge.AddDocument;

/// <summary>
/// Command to add a new knowledge document with automatic embedding generation
/// </summary>
public class AddKnowledgeDocumentCommand : IRequest<KnowledgeDocumentResponse>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Category { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; } = true;
}
