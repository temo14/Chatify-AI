namespace ChatAI.Application.Interfaces;

/// <summary>
/// Cache service interface for storing and retrieving cached data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached item by key
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Set cached item with expiration
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Remove item from cache
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Get or create cached item if it doesn't exist
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Clear all cached items
    /// </summary>
    void Clear();
}
