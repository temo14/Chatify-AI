using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Service for reading and applying PLATFORM-WIDE runtime configuration from database
/// Allows real-time configuration changes without redeployment
/// 
/// IMPORTANT: This service uses a GLOBAL CACHE without tenant isolation
/// DO NOT USE for tenant-specific settings like Temperature, MaxTokens, SupportEmail, etc.
/// 
/// CORRECT USAGE: Platform-wide settings that apply to all tenants
/// - Global rate limits
/// - Feature flags for entire platform
/// - External API keys (not tenant-specific)
/// - System-wide defaults
/// 
/// INCORRECT USAGE: Tenant-specific settings
/// - Use TenantSettings entity instead
/// - Access via ITenantRepository.GetByIdAsync(tenantId)
/// 
/// Caching: 5-minute cache is appropriate for platform settings that rarely change
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _configRepository;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly Dictionary<string, AdminConfiguration> _cache = new();
    private DateTime _lastCacheRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public ConfigurationService(
        IConfigurationRepository configRepository,
        ILogger<ConfigurationService> logger)
    {
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get configuration value with fallback to default
    /// </summary>
    public async Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        try
        {
            await RefreshCacheIfNeededAsync(ct);

            if (_cache.TryGetValue(key, out var config) && config.IsActive)
            {
                return ConvertValue<T>(config.Value, defaultValue);
            }

            _logger.LogDebug("Configuration '{Key}' not found, using default: {Default}", key, defaultValue);
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving configuration '{Key}', using default: {Default}", key, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// Get multiple configurations by category
    /// </summary>
    public async Task<Dictionary<string, string>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        await RefreshCacheIfNeededAsync(ct);

        return _cache
            .Where(kvp => kvp.Value.Category == category && kvp.Value.IsActive)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
    }

    /// <summary>
    /// Force cache refresh
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var configs = await _configRepository.GetActiveConfigurationsAsync(ct);
        
        _cache.Clear();
        foreach (var config in configs)
        {
            _cache[config.Key] = config;
        }

        _lastCacheRefresh = DateTime.UtcNow;
        _logger.LogInformation("✅ Configuration cache refreshed with {Count} settings", _cache.Count);
    }

    private async Task RefreshCacheIfNeededAsync(CancellationToken ct)
    {
        if (DateTime.UtcNow - _lastCacheRefresh > CacheExpiration || _cache.Count == 0)
        {
            await RefreshAsync(ct);
        }
    }

    private T ConvertValue<T>(string value, T defaultValue)
    {
        try
        {
            var targetType = typeof(T);

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            if (targetType == typeof(string))
                return (T)(object)value;

            if (targetType == typeof(int))
                return (T)(object)int.Parse(value);

            if (targetType == typeof(double) || targetType == typeof(float))
                return (T)(object)double.Parse(value);

            if (targetType == typeof(bool))
                return (T)(object)bool.Parse(value);

            if (targetType == typeof(long))
                return (T)(object)long.Parse(value);

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert configuration value '{Value}' to type {Type}", value, typeof(T).Name);
            return defaultValue;
        }
    }

    /// <summary>
    /// Get all AI settings for chat execution
    /// NOTE: This returns GLOBAL defaults. For tenant-specific AI settings,
    /// use tenant.Settings.Temperature, tenant.Settings.MaxTokens, etc.
    /// </summary>
    public async Task<AIChatSettings> GetAISettingsAsync(CancellationToken ct = default)
    {
        return new AIChatSettings
        {
            SystemPrompt = await GetValueAsync("AI.SystemPrompt", "You are a helpful AI assistant.", ct),
            Temperature = await GetValueAsync("AI.Temperature", 0.7, ct),
            MaxTokens = await GetValueAsync("AI.MaxTokens", 1500, ct),
            TopP = await GetValueAsync("AI.TopP", 0.95, ct),
            FrequencyPenalty = await GetValueAsync("AI.FrequencyPenalty", 0.3, ct),
            PresencePenalty = await GetValueAsync("AI.PresencePenalty", 0.2, ct),
            ModelName = await GetValueAsync("AI.ModelName", "gpt-4o", ct)
        };
    }

    /// <summary>
    /// Get RAG settings
    /// </summary>
    public async Task<RAGSettings> GetRAGSettingsAsync(CancellationToken ct = default)
    {
        return new RAGSettings
        {
            Enabled = await GetValueAsync("RAG.Enabled", true, ct),
            TopKResults = await GetValueAsync("RAG.TopKResults", 3, ct),
            ScoreThreshold = await GetValueAsync("RAG.ScoreThreshold", 0.7, ct),
            MaxContextLength = await GetValueAsync("RAG.MaxContextLength", 3000, ct),
            DocumentChunkSize = await GetValueAsync("RAG.DocumentChunkSize", 800, ct),
            ChunkOverlap = await GetValueAsync("RAG.ChunkOverlap", 150, ct)
        };
    }
}
