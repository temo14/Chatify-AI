using FluentValidation;

namespace ChatAI.Application.Features.Auth.ValidateApiKey;

public class ValidateApiKeyQueryValidator : AbstractValidator<ValidateApiKeyQuery>
{
    public ValidateApiKeyQueryValidator()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API key is required")
            .MinimumLength(10).WithMessage("API key must be at least 10 characters")
            .Must(key => key.StartsWith("chatai_", StringComparison.OrdinalIgnoreCase))
            .WithMessage("API key must start with 'chatai_'");
    }
}
