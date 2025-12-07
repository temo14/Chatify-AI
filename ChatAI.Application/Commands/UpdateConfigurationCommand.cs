using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to create or update a configuration setting (Write operation)
/// </summary>
public record UpdateConfigurationCommand : IRequest<Guid>
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string DataType { get; init; } = "String"; // String, Integer, Boolean, JSON
    public string? Category { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public string? ModifiedBy { get; init; }
    public string? ValidationRule { get; init; }
}
