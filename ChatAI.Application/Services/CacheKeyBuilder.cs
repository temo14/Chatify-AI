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
    /// </summary>
    public static string EmbeddingFromContent(string content)
    {
        var contentHash = ComputeHash(content);
        return $"{Prefix}:embedding:{contentHash}";
    }

    /// <summary>
    /// Generate cache key for conversation history
    /// </summary>
    public static string ConversationHistory(string sessionId)
    {
        return $"{Prefix}:history:{sessionId}";
    }

    /// <summary>
    /// Generate cache key for knowledge search results
    /// </summary>
    public static string KnowledgeSearch(string query, int limit)
    {
        var queryHash = ComputeHash(query);
        return $"{Prefix}:search:{queryHash}:{limit}";
    }

    /// <summary>
    /// Generate cache key for user sessions
    /// </summary>
    public static string UserSessions(string userId)
    {
        return $"{Prefix}:sessions:{userId}";
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
