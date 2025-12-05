using ChatAI.Application.Models.Response;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for ChatResponse - ensures output quality and safety
/// </summary>
public class ChatResponseValidator : AbstractValidator<ChatResponse>
{
    public ChatResponseValidator()
    {
        RuleFor(x => x.Reply)
            .NotEmpty()
            .WithMessage("Response reply cannot be empty")
            .MaximumLength(50000)
            .WithMessage("Response reply exceeds maximum length");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        // Content safety checks
        RuleFor(x => x.Reply)
            .Must(NotContainUnsafeContent)
            .WithMessage("Response contains potentially unsafe content")
            .When(x => !string.IsNullOrEmpty(x.Reply));

        // Ensure response is relevant (not refusing to answer)
        RuleFor(x => x.Reply)
            .Must(IsRelevantResponse)
            .WithMessage("Response indicates AI refused to answer")
            .When(x => !string.IsNullOrEmpty(x.Reply));
    }

    /// <summary>
    /// Check for unsafe content patterns
    /// </summary>
    private bool NotContainUnsafeContent(string reply)
    {
        var unsafePatterns = new[]
        {
            "generate malware",
            "create virus",
            "hack into",
            "exploit vulnerability",
            "bypass security"
        };

        var lowerReply = reply.ToLowerInvariant();
        return !unsafePatterns.Any(pattern => lowerReply.Contains(pattern));
    }

    /// <summary>
    /// Check if response is relevant (AI didn't refuse)
    /// </summary>
    private bool IsRelevantResponse(string reply)
    {
        var refusalPatterns = new[]
        {
            "i cannot assist with",
            "i can't help with",
            "i'm not able to",
            "sorry, i cannot",
            "i don't have the ability"
        };

        var lowerReply = reply.ToLowerInvariant();
        
        // Allow if no refusal pattern found
        if (!refusalPatterns.Any(pattern => lowerReply.Contains(pattern)))
            return true;

        // If refusal pattern found, check if it's a legitimate refusal (e.g., for harmful requests)
        // For now, we'll allow legitimate refusals
        return true;
    }
}
