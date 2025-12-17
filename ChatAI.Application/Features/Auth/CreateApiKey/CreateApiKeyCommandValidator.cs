
using FluentValidation;

namespace ChatAI.Application.Features.Auth.CreateApiKey;

public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(200).WithMessage("Client name cannot exceed 200 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
            
        RuleFor(x => x.RateLimitPerMinute)
            .GreaterThan(0).WithMessage("Rate limit per minute must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Rate limit per minute cannot exceed 1000");
            
        RuleFor(x => x.RateLimitPerDay)
            .GreaterThan(0).WithMessage("Rate limit per day must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("Rate limit per day cannot exceed 100000");
            
        RuleFor(x => x.ExpiresAt)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future");
    }
}
