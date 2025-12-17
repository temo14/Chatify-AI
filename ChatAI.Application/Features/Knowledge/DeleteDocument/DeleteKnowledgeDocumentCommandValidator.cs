
using FluentValidation;

namespace ChatAI.Application.Features.Knowledge.DeleteDocument;

/// <summary>
/// Validator for DeleteKnowledgeDocumentCommand - ensures valid document ID
/// </summary>
public class DeleteKnowledgeDocumentCommandValidator : AbstractValidator<DeleteKnowledgeDocumentCommand>
{
    public DeleteKnowledgeDocumentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Document ID is required");
    }
}
