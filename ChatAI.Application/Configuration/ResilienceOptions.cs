namespace ChatAI.Application.Configuration;

/// <summary>
/// Configuration for resilience patterns (retry, circuit breaker, timeout)
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Number of retry attempts for transient failures
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff (in milliseconds)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Circuit breaker failure threshold (number of consecutive failures before opening)
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker open duration (in seconds)
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Overall operation timeout (in seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable resilience policies
    /// </summary>
    public bool Enabled { get; set; } = true;
}
