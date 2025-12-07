using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to delete a knowledge document and its embeddings
/// </summary>
public class DeleteKnowledgeDocumentCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
