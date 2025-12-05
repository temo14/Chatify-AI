namespace ChatAI.Application.Configuration;

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Enable or disable caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache expiration in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Conversation history cache expiration in minutes
    /// </summary>
    public int ConversationExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Embedding cache expiration in hours (embeddings rarely change)
    /// </summary>
    public int EmbeddingExpirationHours { get; set; } = 24;

    /// <summary>
    /// Tool result cache expiration in minutes
    /// </summary>
    public int ToolResultExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of cached items (memory limit)
    /// </summary>
    public int MaxCachedItems { get; set; } = 10000;
}
