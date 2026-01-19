using ChatAI.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// In-memory implementation of webhook queue for local development
/// WARNING: Not durable - messages lost on restart
/// </summary>
public class InMemoryMetaWebhookQueue : IMetaWebhookQueue
{
    private readonly ConcurrentQueue<MetaWebhookMessage> _queue = new();
    private readonly ILogger<InMemoryMetaWebhookQueue> _logger;
    
    public InMemoryMetaWebhookQueue(ILogger<InMemoryMetaWebhookQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Task EnqueueAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(message);
        _logger.LogDebug("Enqueued webhook message {EventKey} for connection {ConnectionId}", 
            message.EventKey, message.ConnectionId);
        return Task.CompletedTask;
    }
    
    public Task<MetaWebhookMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_queue.TryDequeue(out var message))
        {
            _logger.LogDebug("Dequeued webhook message {EventKey} for connection {ConnectionId}", 
                message.EventKey, message.ConnectionId);
            return Task.FromResult<MetaWebhookMessage?>(message);
        }
        
        return Task.FromResult<MetaWebhookMessage?>(null);
    }
    
    public Task CompleteAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Completed webhook message {EventKey}", message.EventKey);
        return Task.CompletedTask;
    }
    
    public Task DeadLetterAsync(MetaWebhookMessage message, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Dead-lettered webhook message {EventKey}: {Reason}", message.EventKey, reason);
        return Task.CompletedTask;
    }
}
