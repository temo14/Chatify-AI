using FluentValidation;

namespace ChatAI.Application.Features.Tenants.UpdateTenantSettings;

/// <summary>
/// Validator for UpdateTenantSettingsCommand
/// </summary>
public class UpdateTenantSettingsCommandValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    public UpdateTenantSettingsCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        When(x => x.Temperature.HasValue, () =>
        {
            RuleFor(x => x.Temperature!.Value)
                .InclusiveBetween(0.0, 2.0)
                .WithMessage("Temperature must be between 0.0 and 2.0");
        });

        When(x => x.MaxTokens.HasValue, () =>
        {
            RuleFor(x => x.MaxTokens!.Value)
                .GreaterThan(0)
                .LessThanOrEqualTo(32000)
                .WithMessage("MaxTokens must be between 1 and 32000");
        });

        When(x => x.ChunkSize.HasValue, () =>
        {
            RuleFor(x => x.ChunkSize!.Value)
                .GreaterThan(0)
                .LessThanOrEqualTo(10000)
                .WithMessage("ChunkSize must be between 1 and 10000");
        });

        When(x => x.ChunkOverlap.HasValue, () =>
        {
            RuleFor(x => x.ChunkOverlap!.Value)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(1000)
                .WithMessage("ChunkOverlap must be between 0 and 1000");
        });

        When(x => x.ChatHistoryRetentionDays.HasValue, () =>
        {
            RuleFor(x => x.ChatHistoryRetentionDays!.Value)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(365)
                .WithMessage("ChatHistoryRetentionDays must be between 1 and 365");
        });

        When(x => !string.IsNullOrEmpty(x.SystemPrompt), () =>
        {
            RuleFor(x => x.SystemPrompt)
                .MaximumLength(5000)
                .WithMessage("SystemPrompt must not exceed 5000 characters");
        });

        When(x => !string.IsNullOrEmpty(x.WelcomeMessage), () =>
        {
            RuleFor(x => x.WelcomeMessage)
                .MaximumLength(1000)
                .WithMessage("WelcomeMessage must not exceed 1000 characters");
        });
    }
}
