using ChatAI.Application.Commands;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for AddKnowledgeDocumentCommand - ensures document quality and safety
/// </summary>
public class AddKnowledgeDocumentCommandValidator : AbstractValidator<AddKnowledgeDocumentCommand>
{
    public AddKnowledgeDocumentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MinimumLength(3)
            .WithMessage("Title must be at least 3 characters long")
            .MaximumLength(500)
            .WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MinimumLength(10)
            .WithMessage("Content must be at least 10 characters long")
            .MaximumLength(50000)
            .WithMessage("Content must not exceed 50,000 characters");

        RuleFor(x => x.Source)
            .MaximumLength(500)
            .WithMessage("Source must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s_-]+$")
            .WithMessage("Category must only contain alphanumeric characters, spaces, hyphens, and underscores")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));

        RuleFor(x => x.MetadataJson)
            .Must(BeValidJsonOrNull)
            .WithMessage("MetadataJson must be valid JSON")
            .When(x => !string.IsNullOrWhiteSpace(x.MetadataJson));
    }

    private bool BeValidJsonOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
