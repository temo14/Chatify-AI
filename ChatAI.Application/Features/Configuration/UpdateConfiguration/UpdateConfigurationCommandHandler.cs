
using ChatAI.Application.Services;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Configuration.UpdateConfiguration;

/// <summary>
/// Handler for UpdateConfigurationCommand - creates or updates a configuration setting
/// NOTE: This is for GLOBAL platform configuration, not tenant-specific settings
/// </summary>
public class UpdateConfigurationCommandHandler : IRequestHandler<UpdateConfigurationCommand, Guid>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ICacheService _cacheService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateConfigurationCommandHandler> _logger;

    public UpdateConfigurationCommandHandler(
        IConfigurationRepository configurationRepository,
        ICacheService cacheService,
        ITenantContext tenantContext,
        ILogger<UpdateConfigurationCommandHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(UpdateConfigurationCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📨 Updating configuration '{Key}'", command.Key);

        var existing = await _configurationRepository.GetByKeyAsync(command.Key, cancellationToken);

        if (existing != null)
        {
            // Update existing configuration
            existing.Value = command.Value;
            existing.DataType = command.DataType;
            existing.Category = command.Category ?? existing.Category;
            existing.Description = command.Description ?? existing.Description;
            existing.IsActive = command.IsActive;
            existing.ModifiedBy = command.ModifiedBy ?? existing.ModifiedBy ?? "System";
            existing.ValidationRule = command.ValidationRule ?? existing.ValidationRule;
            existing.UpdatedAt = DateTime.UtcNow;

            await _configurationRepository.UpdateAsync(existing, cancellationToken);

            // Invalidate AI settings cache if AI configuration is updated
            if (command.Key.StartsWith("AI.", StringComparison.OrdinalIgnoreCase))
            {
                var aiSettingsCacheKey = CacheKeyBuilder.AISettings(_tenantContext.RequiredTenantId);
                _cacheService.Remove(aiSettingsCacheKey);
                _logger.LogInformation("🗑️ Invalidated AI settings cache after updating '{Key}'", command.Key);
            }

            _logger.LogInformation("✅ Updated configuration '{Key}'", command.Key);
            return existing.Id;
        }

        // Create new configuration
        var config = new AdminConfiguration
        {
            Id = Guid.NewGuid(),
            Key = command.Key,
            Value = command.Value,
            DataType = command.DataType,
            Category = command.Category ?? "General",
            Description = command.Description ?? string.Empty,
            IsActive = command.IsActive,
            ModifiedBy = command.ModifiedBy ?? "System",
            ValidationRule = command.ValidationRule,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _configurationRepository.AddAsync(config, cancellationToken);

        // Invalidate AI settings cache if AI configuration is created
        if (command.Key.StartsWith("AI.", StringComparison.OrdinalIgnoreCase))
        {
            var aiSettingsCacheKey = CacheKeyBuilder.AISettings(_tenantContext.RequiredTenantId);
            _cacheService.Remove(aiSettingsCacheKey);
            _logger.LogInformation("🗑️ Invalidated AI settings cache after creating '{Key}'", command.Key);
        }

        _logger.LogInformation("✅ Created configuration '{Key}'", command.Key);
        return result.Id;
    }
}
