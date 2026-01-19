namespace ChatAI.Domain.Entities;

/// <summary>
/// Tracks received Meta messages for deduplication
/// Meta webhooks are delivered at-least-once and may arrive out of order
/// </summary>
public class MetaInboundDedupe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Connection that received this message
    /// </summary>
    public Guid ConnectionId { get; set; }
    
    /// <summary>
    /// Meta's unique message identifier
    /// - Messenger: mid
    /// - WhatsApp: message id
    /// - Instagram: message id from event payload
    /// </summary>
    public string MetaMessageId { get; set; } = string.Empty;
    
    /// <summary>
    /// When this message was first received
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property
    /// </summary>
    public MetaChannelConnection? Connection { get; set; }
}
