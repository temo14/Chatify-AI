using MediatR;

namespace ChatAI.Application.Features.Configuration.DeleteConfiguration;

/// <summary>
/// Command to delete a configuration setting (Write operation)
/// </summary>
public record DeleteConfigurationCommand : IRequest<bool>
{
    public string Key { get; init; } = string.Empty;
}
