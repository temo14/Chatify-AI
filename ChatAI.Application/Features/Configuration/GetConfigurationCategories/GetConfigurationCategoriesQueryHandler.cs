using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;

using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Configuration.GetConfigurationCategories;

/// <summary>
/// Handler for GetConfigurationCategoriesQuery - retrieves all distinct categories
/// </summary>
public class GetConfigurationCategoriesQueryHandler : IRequestHandler<GetConfigurationCategoriesQuery, IEnumerable<string>>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<GetConfigurationCategoriesQueryHandler> _logger;

    public GetConfigurationCategoriesQueryHandler(
        IConfigurationRepository configurationRepository,
        ILogger<GetConfigurationCategoriesQueryHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<string>> Handle(GetConfigurationCategoriesQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Getting configuration categories");

        var categories = await _configurationRepository.GetCategoriesAsync(cancellationToken);

        _logger.LogInformation("✅ Retrieved {Count} categories", categories.Count());

        return categories;
    }
}
