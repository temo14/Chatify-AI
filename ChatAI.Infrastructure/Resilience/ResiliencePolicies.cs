using ChatAI.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ChatAI.Infrastructure.Resilience;

/// <summary>
/// Centralized resilience policies for external service calls
/// Implements retry, circuit breaker, and timeout patterns
/// </summary>
public class ResiliencePolicies
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResiliencePolicies> _logger;

    public ResiliencePolicies(
        IOptions<ResilienceOptions> options,
        ILogger<ResiliencePolicies> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retry policy with exponential backoff for transient failures
    /// </summary>
    public ResiliencePipeline<TResult> GetRetryPolicy<TResult>(string operationName)
    {
        if (!_options.Enabled)
        {
            return new ResiliencePipelineBuilder<TResult>().Build();
        }

        return new ResiliencePipelineBuilder<TResult>()
            .AddRetry(new RetryStrategyOptions<TResult>
            {
                MaxRetryAttempts = _options.RetryCount,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {Attempt}/{MaxAttempts} for {Operation}. Delay: {Delay}ms. Exception: {Exception}",
                        args.AttemptNumber + 1,
                        _options.RetryCount,
                        operationName,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures
    /// Opens circuit after consecutive failures, closes after success
    /// </summary>
    public ResiliencePipeline<TResult> GetCircuitBreakerPolicy<TResult>(string operationName)
    {
        if (!_options.Enabled)
        {
            return new ResiliencePipelineBuilder<TResult>().Build();
        }

        return new ResiliencePipelineBuilder<TResult>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
            {
                FailureRatio = 0.5, // Open if 50% of calls fail
                MinimumThroughput = _options.CircuitBreakerThreshold,
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker OPENED for {Operation}. Will retry after {Duration}s. Exception: {Exception}",
                        operationName,
                        _options.CircuitBreakerDurationSeconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker CLOSED for {Operation}. Service recovered.",
                        operationName);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker HALF-OPEN for {Operation}. Testing service...",
                        operationName);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Timeout policy to prevent hanging operations
    /// </summary>
    public ResiliencePipeline<TResult> GetTimeoutPolicy<TResult>(string operationName)
    {
        if (!_options.Enabled)
        {
            return new ResiliencePipelineBuilder<TResult>().Build();
        }

        return new ResiliencePipelineBuilder<TResult>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogError(
                        "Operation {Operation} timed out after {Timeout}s",
                        operationName,
                        _options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Combined policy: Timeout → Retry → Circuit Breaker
    /// This is the recommended policy for most external service calls
    /// </summary>
    public ResiliencePipeline<TResult> GetCombinedPolicy<TResult>(string operationName)
    {
        if (!_options.Enabled)
        {
            return new ResiliencePipelineBuilder<TResult>().Build();
        }

        return new ResiliencePipelineBuilder<TResult>()
            // 1. Timeout first (innermost)
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogError("⏱️ Timeout: {Operation} exceeded {Timeout}s", 
                        operationName, _options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            // 2. Retry with exponential backoff
            .AddRetry(new RetryStrategyOptions<TResult>
            {
                MaxRetryAttempts = _options.RetryCount,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("🔄 Retry {Attempt}/{Max} for {Operation} after {Delay}ms",
                        args.AttemptNumber + 1,
                        _options.RetryCount,
                        operationName,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            // 3. Circuit breaker (outermost)
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
            {
                FailureRatio = 0.5,
                MinimumThroughput = _options.CircuitBreakerThreshold,
                BreakDuration = TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds),
                OnOpened = args =>
                {
                    _logger.LogError("🔴 Circuit OPENED for {Operation} - too many failures", operationName);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("🟢 Circuit CLOSED for {Operation} - service recovered", operationName);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("🟡 Circuit HALF-OPEN for {Operation} - testing...", operationName);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
