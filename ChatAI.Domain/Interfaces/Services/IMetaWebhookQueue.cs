using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Message queued for webhook processing
/// </summary>
public class MetaWebhookMessage
{
    public Guid ConnectionId { get; set; }
    public MetaChannel Channel { get; set; }
    /// <summary>
    /// Sender/external user identifier from Meta payload (e.g., PSID, IG user id, WhatsApp wa_id).
    /// Used to set Service Bus SessionId for per-conversation ordering.
    /// </summary>
    public string? ExternalUserId { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string EventKey { get; set; } = Guid.NewGuid().ToString(); // For tracing
}

/// <summary>
/// Queue abstraction for webhook processing
/// Allows switching between Azure Service Bus (production) and in-memory (development)
/// </summary>
public interface IMetaWebhookQueue
{
    /// <summary>
    /// Enqueue a webhook for background processing
    /// </summary>
    Task EnqueueAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dequeue and process webhooks (called by background worker)
    /// </summary>
    Task<MetaWebhookMessage?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Complete processing of a message (remove from queue)
    /// </summary>
    Task CompleteAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dead-letter a message (move to DLQ after max retries)
    /// </summary>
    Task DeadLetterAsync(MetaWebhookMessage message, string reason, CancellationToken cancellationToken = default);
}
