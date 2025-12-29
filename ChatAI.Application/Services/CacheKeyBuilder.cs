using System.Security.Cryptography;
using System.Text;

namespace ChatAI.Application.Services;

/// <summary>
/// Builds consistent cache keys for different types of cached data
/// Uses hashing for content-based keys to avoid key length issues
/// </summary>
public static class CacheKeyBuilder
{
    private const string Prefix = "chatai";

    /// <summary>
    /// Generate cache key for embeddings based on content hash
    /// Tenant-isolated to prevent cross-tenant embedding reuse
    /// </summary>
    public static string EmbeddingFromContent(string content, Guid tenantId)
    {
        var contentHash = ComputeHash(content);
        return $"{Prefix}:t:{tenantId}:embedding:{contentHash}";
    }

    /// <summary>
    /// Generate cache key for conversation history
    /// Tenant-isolated to prevent cross-tenant session access
    /// </summary>
    public static string ConversationHistory(string sessionId, Guid tenantId)
    {
        return $"{Prefix}:t:{tenantId}:history:{sessionId}";
    }

    /// <summary>
    /// Generate cache key for knowledge search results
    /// Tenant-isolated to prevent cross-tenant knowledge leakage
    /// </summary>
    public static string KnowledgeSearch(string query, int limit, Guid tenantId)
    {
        var queryHash = ComputeHash(query);
        return $"{Prefix}:t:{tenantId}:search:{queryHash}:{limit}";
    }

    /// <summary>
    /// Generate cache key for user sessions
    /// Tenant-isolated to prevent cross-tenant user data access
    /// </summary>
    public static string UserSessions(string userId, Guid tenantId)
    {
        return $"{Prefix}:t:{tenantId}:sessions:{userId}";
    }

    /// <summary>
    /// Generate cache key for AI settings
    /// Tenant-isolated to prevent shared configuration across tenants
    /// </summary>
    public static string AISettings(Guid tenantId)
    {
        return $"{Prefix}:t:{tenantId}:config:ai-settings";
    }

    /// <summary>
    /// Compute SHA256 hash of input string for cache key generation
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // Use first 16 characters (8 bytes)
    }
}
