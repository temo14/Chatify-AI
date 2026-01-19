namespace ChatAI.Domain.Entities;

/// <summary>
/// Maps external Meta user IDs to internal Chatify session IDs
/// Provides stable conversation continuity across messages
/// </summary>
public class MetaConversationMap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Meta channel connection
    /// </summary>
    public Guid ConnectionId { get; set; }
    
    /// <summary>
    /// External user identifier from Meta
    /// - Messenger: PSID (Page-Scoped ID)
    /// - WhatsApp: wa_id (WhatsApp ID, phone number format)
    /// - Instagram: Instagram user ID
    /// </summary>
    public string ExternalUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Internal Chatify session ID
    /// </summary>
    public string ChatSessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// When this mapping was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last time this conversation was active
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the user has opted out (STOP)
    /// </summary>
    public bool IsOptedOut { get; set; } = false;

    /// <summary>
    /// When the user opted out
    /// </summary>
    public DateTime? OptedOutAt { get; set; }

    /// <summary>
    /// When the user opted back in (START)
    /// </summary>
    public DateTime? OptedInAt { get; set; }
    
    /// <summary>
    /// Navigation property
    /// </summary>
    public MetaChannelConnection? Connection { get; set; }
}
