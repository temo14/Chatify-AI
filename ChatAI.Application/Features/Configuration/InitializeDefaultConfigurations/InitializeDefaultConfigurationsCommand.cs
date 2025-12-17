using MediatR;

namespace ChatAI.Application.Features.Configuration.InitializeDefaultConfigurations;

/// <summary>
/// Command to initialize default configuration settings (Write operation)
/// </summary>
public record InitializeDefaultConfigurationsCommand : IRequest<int>
{
    public string? ModifiedBy { get; init; }
}
