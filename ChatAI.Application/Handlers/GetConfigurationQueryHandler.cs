using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for GetConfigurationQuery - retrieves a single configuration by key
/// </summary>
public class GetConfigurationQueryHandler : IRequestHandler<GetConfigurationQuery, AdminConfiguration?>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<GetConfigurationQueryHandler> _logger;

    public GetConfigurationQueryHandler(
        IConfigurationRepository configurationRepository,
        ILogger<GetConfigurationQueryHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdminConfiguration?> Handle(GetConfigurationQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Getting configuration '{Key}'", query.Key);

        var config = await _configurationRepository.GetByKeyAsync(query.Key, cancellationToken);

        if (config == null)
        {
            _logger.LogWarning("⚠️ Configuration '{Key}' not found", query.Key);
        }

        return config;
    }
}
