using ChatAI.Application.Commands;
using FluentValidation;

namespace ChatAI.Application.Validators;

/// <summary>
/// Validator for SendChatCommand - ensures input safety and quality
/// </summary>
public class SendChatCommandValidator : AbstractValidator<SendChatCommand>
{
    public SendChatCommandValidator()
    {
        // User ID validation
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .MaximumLength(100)
            .WithMessage("User ID must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("User ID must only contain alphanumeric characters, hyphens, and underscores");

        // Message validation
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required")
            .MinimumLength(1)
            .WithMessage("Message must be at least 1 character long")
            .MaximumLength(10000)
            .WithMessage("Message must not exceed 10,000 characters");

        // Prompt injection detection
        RuleFor(x => x.Message)
            .Must(NotContainPromptInjection)
            .WithMessage("Message contains potentially unsafe content")
            .When(x => !string.IsNullOrEmpty(x.Message));

        // Session ID validation (optional field)
        RuleFor(x => x.SessionId)
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SessionId));
    }

    /// <summary>
    /// Detect common prompt injection patterns
    /// </summary>
    private bool NotContainPromptInjection(string message)
    {
        // Common prompt injection patterns
        var suspiciousPatterns = new[]
        {
            "ignore previous instructions",
            "ignore all previous",
            "disregard previous",
            "forget everything",
            "new instructions:",
            "system:",
            "assistant:",
            "[INST]",
            "###",
            "<|system|>",
            "<|assistant|>",
            "jailbreak",
            "DAN mode",
            "developer mode"
        };

        var lowerMessage = message.ToLowerInvariant();
        return !suspiciousPatterns.Any(pattern => lowerMessage.Contains(pattern.ToLowerInvariant()));
    }
}
