using FluentValidation;

namespace ChatAI.Application.Features.Tenants.CreateTenant;

/// <summary>
/// Validator for CreateTenantCommand
/// Ensures tenant creation includes valid admin credentials
/// </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Tenant slug is required")
            .MinimumLength(3)
            .WithMessage("Slug must be at least 3 characters")
            .MaximumLength(50)
            .WithMessage("Slug must not exceed 50 characters")
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.PlanTier)
            .NotEmpty()
            .WithMessage("Plan tier is required")
            .Must(tier => new[] { "Free", "Starter", "Pro", "Enterprise" }.Contains(tier))
            .WithMessage("Plan tier must be Free, Starter, Pro, or Enterprise");

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .WithMessage("Admin password is required - every tenant needs an admin user who can log in")
            .MinimumLength(8)
            .WithMessage("Admin password must be at least 8 characters")
            .MaximumLength(100)
            .WithMessage("Admin password must not exceed 100 characters");

        When(x => !string.IsNullOrEmpty(x.AdminFullName), () =>
        {
            RuleFor(x => x.AdminFullName)
                .MaximumLength(200)
                .WithMessage("Admin full name must not exceed 200 characters");
        });

        When(x => !string.IsNullOrEmpty(x.CustomDomain), () =>
        {
            RuleFor(x => x.CustomDomain)
                .Matches(@"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?(\.[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?)*$")
                .WithMessage("Invalid domain format");
        });

        When(x => x.MaxDocuments.HasValue, () =>
        {
            RuleFor(x => x.MaxDocuments!.Value)
                .GreaterThan(0)
                .WithMessage("Max documents must be greater than 0")
                .LessThanOrEqualTo(10000)
                .WithMessage("Max documents must not exceed 10000");
        });

        When(x => x.MaxMonthlyMessages.HasValue, () =>
        {
            RuleFor(x => x.MaxMonthlyMessages!.Value)
                .GreaterThan(0)
                .WithMessage("Max monthly messages must be greater than 0")
                .LessThanOrEqualTo(1000000)
                .WithMessage("Max monthly messages must not exceed 1000000");
        });
    }
}
