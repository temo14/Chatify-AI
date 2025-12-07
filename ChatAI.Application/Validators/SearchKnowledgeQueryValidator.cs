using ChatAI.Application.Queries;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for SearchKnowledgeQuery - ensures search parameters are valid
/// </summary>
public class SearchKnowledgeQueryValidator : AbstractValidator<SearchKnowledgeQuery>
{
    public SearchKnowledgeQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters long")
            .MaximumLength(1000)
            .WithMessage("Search query must not exceed 1,000 characters");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(50)
            .WithMessage("Limit must not exceed 50");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s_-]+$")
            .WithMessage("Category must only contain alphanumeric characters, spaces, hyphens, and underscores")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));
    }
}
