using ChatAI.Application.Configuration;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// In-memory cache service implementation with metrics logging
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly CacheOptions _options;
    private long _hits = 0;
    private long _misses = 0;

    public MemoryCacheService(
        IMemoryCache cache,
        ILogger<MemoryCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public T? Get<T>(string key)
    {
        if (!_options.Enabled)
        {
            return default;
        }

        if (_cache.TryGetValue(key, out T? value))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("💾 Cache HIT for key: {Key}", key);
            return value;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("❌ Cache MISS for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(cacheExpiration)
            .SetSize(1) // Each item counts as 1 toward size limit
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("🗑️ Cache eviction: {Key}, Reason: {Reason}", key, reason);
            });

        _cache.Set(key, value, cacheEntryOptions);
        _logger.LogDebug("💾 Cache SET for key: {Key}, Expiration: {Expiration}s", key, cacheExpiration.TotalSeconds);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("🗑️ Cache REMOVE for key: {Key}", key);
    }

    public bool Exists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (!_options.Enabled)
        {
            return await factory();
        }

        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("💾 Cache HIT for key: {Key}", key);
            return cachedValue!;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("❌ Cache MISS for key: {Key}, creating...", key);

        var value = await factory();
        Set(key, value, expiration);

        LogCacheStats();
        
        return value;
    }

    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove all entries
            _logger.LogInformation("🗑️ Cache cleared");
        }
    }

    private void LogCacheStats()
    {
        var total = _hits + _misses;
        if (total > 0 && total % 100 == 0) // Log every 100 requests
        {
            var hitRate = (_hits / (double)total) * 100;
            _logger.LogInformation("📊 Cache Stats: Hits={Hits}, Misses={Misses}, HitRate={HitRate:F1}%", 
                _hits, _misses, hitRate);
        }
    }
}
