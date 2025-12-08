using ChatAI.Application.Queries;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for ExportSessionQuery
/// </summary>
public class ExportSessionQueryValidator : AbstractValidator<ExportSessionQuery>
{
    public ExportSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required")
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid export format");
    }
}
