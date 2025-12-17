
using FluentValidation;

namespace ChatAI.Application.Features.Feedback.SubmitFeedback;

/// <summary>
/// Validator for SubmitFeedbackCommand
/// </summary>
public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("Message ID is required");

        RuleFor(x => x.Rating)
            .Must(r => r == 1 || r == -1)
            .WithMessage("Rating must be 1 (thumbs up) or -1 (thumbs down)");

        RuleFor(x => x.UserId)
            .MaximumLength(100)
            .WithMessage("User ID must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.UserId));

        RuleFor(x => x.SessionId)
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SessionId));

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage("Comment must not exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid feedback category")
            .When(x => x.Category.HasValue);
    }
}
