using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;

using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Configuration.GetConfigurationList;

/// <summary>
/// Handler for GetConfigurationListQuery - retrieves configurations with optional filters
/// </summary>
public class GetConfigurationListQueryHandler : IRequestHandler<GetConfigurationListQuery, IEnumerable<AdminConfiguration>>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<GetConfigurationListQueryHandler> _logger;

    public GetConfigurationListQueryHandler(
        IConfigurationRepository configurationRepository,
        ILogger<GetConfigurationListQueryHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<AdminConfiguration>> Handle(GetConfigurationListQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Getting configuration list - Category: {Category}, Active: {IsActive}", 
            query.Category ?? "all", query.IsActive?.ToString() ?? "all");

        IEnumerable<AdminConfiguration> configs;

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            configs = await _configurationRepository.GetByCategoryAsync(query.Category, cancellationToken);
        }
        else if (query.IsActive == true)
        {
            configs = await _configurationRepository.GetActiveConfigurationsAsync(cancellationToken);
        }
        else
        {
            configs = await _configurationRepository.GetAllAsync(cancellationToken);
        }

        // Apply active filter if specified and category was used
        if (query.IsActive.HasValue && !string.IsNullOrWhiteSpace(query.Category))
        {
            configs = configs.Where(c => c.IsActive == query.IsActive.Value);
        }

        _logger.LogInformation("✅ Retrieved {Count} configuration items", configs.Count());

        return configs;
    }
}
