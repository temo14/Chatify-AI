using ChatAI.Application.Queries;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for GetUserSessionsQuery
/// </summary>
public class GetUserSessionsQueryValidator : AbstractValidator<GetUserSessionsQuery>
{
    public GetUserSessionsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .MaximumLength(100)
            .WithMessage("User ID must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("User ID must only contain alphanumeric characters, hyphens, and underscores");
    }
}
