
using FluentValidation;

namespace ChatAI.Application.Features.Configuration.UpdateConfiguration;

/// <summary>
/// Validator for UpdateConfigurationCommand
/// </summary>
public class UpdateConfigurationCommandValidator : AbstractValidator<UpdateConfigurationCommand>
{
    private static readonly string[] AllowedDataTypes = { "String", "Integer", "Boolean", "JSON", "Double" };

    public UpdateConfigurationCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .WithMessage("Configuration key is required")
            .MaximumLength(200)
            .WithMessage("Key must not exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Key must only contain alphanumeric characters, dots, hyphens, and underscores");

        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage("Value is required")
            .MaximumLength(50000)
            .WithMessage("Value must not exceed 50000 characters");

        RuleFor(x => x.DataType)
            .Must(dt => AllowedDataTypes.Contains(dt))
            .WithMessage($"DataType must be one of: {string.Join(", ", AllowedDataTypes)}");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ModifiedBy)
            .MaximumLength(100)
            .WithMessage("ModifiedBy must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.ModifiedBy));

        RuleFor(x => x.ValidationRule)
            .MaximumLength(200)
            .WithMessage("ValidationRule must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.ValidationRule));

        // Validate value based on data type
        RuleFor(x => x)
            .Must(cmd => ValidateValueForDataType(cmd.Value, cmd.DataType))
            .WithMessage(cmd => $"Value '{cmd.Value}' is not valid for data type '{cmd.DataType}'");
    }

    private bool ValidateValueForDataType(string value, string dataType)
    {
        return dataType switch
        {
            "Integer" => int.TryParse(value, out _),
            "Double" => double.TryParse(value, out _),
            "Boolean" => bool.TryParse(value, out _),
            _ => true // String and JSON accept any value
        };
    }
}
