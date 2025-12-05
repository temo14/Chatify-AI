using ChatAI.Application.Queries;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for GetConversationHistoryQuery
/// </summary>
public class GetConversationHistoryQueryValidator : AbstractValidator<GetConversationHistoryQuery>
{
    public GetConversationHistoryQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required")
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters");

        RuleFor(x => x.MaxMessages)
            .GreaterThan(0)
            .WithMessage("MaxMessages must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("MaxMessages must not exceed 1000");
    }
}
