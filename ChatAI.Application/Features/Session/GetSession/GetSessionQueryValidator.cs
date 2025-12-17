
using FluentValidation;

namespace ChatAI.Application.Features.Session.GetSession;

/// <summary>
/// Validator for GetSessionQuery
/// </summary>
public class GetSessionQueryValidator : AbstractValidator<GetSessionQuery>
{
    public GetSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required")
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters");
    }
}
