using ChatAI.Domain.Models;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Service for reading and applying runtime configuration from database
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get configuration value with fallback to default
    /// </summary>
    Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken ct = default);

    /// <summary>
    /// Get all AI settings for chat execution
    /// </summary>
    Task<AIChatSettings> GetAISettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get RAG settings
    /// </summary>
    Task<RAGSettings> GetRAGSettingsAsync(CancellationToken ct = default);
}
