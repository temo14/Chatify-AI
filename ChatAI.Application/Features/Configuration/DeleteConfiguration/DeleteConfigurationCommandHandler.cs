
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Configuration.DeleteConfiguration;

/// <summary>
/// Handler for DeleteConfigurationCommand - deletes a configuration setting
/// </summary>
public class DeleteConfigurationCommandHandler : IRequestHandler<DeleteConfigurationCommand, bool>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<DeleteConfigurationCommandHandler> _logger;

    public DeleteConfigurationCommandHandler(
        IConfigurationRepository configurationRepository,
        ILogger<DeleteConfigurationCommandHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(DeleteConfigurationCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🗑️ Deleting configuration '{Key}'", command.Key);

        var config = await _configurationRepository.GetByKeyAsync(command.Key, cancellationToken);
        if (config == null)
        {
            _logger.LogWarning("⚠️ Configuration '{Key}' not found", command.Key);
            return false;
        }

        await _configurationRepository.DeleteAsync(config.Id, cancellationToken);
        _logger.LogInformation("✅ Configuration '{Key}' deleted successfully", command.Key);
        return true;
    }
}
