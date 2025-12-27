namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a chat conversation session in Chatify AI
/// Simple design: Optional user (for future auth), session tracking
/// </summary>
public class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Tenant (customer) that owns this session
    /// </summary>
    public Guid TenantId { get; set; }
    
    // Optional user (for future when you add authentication)
    public string? UserId { get; set; } // Nullable - most chats are anonymous
    
    // Session metadata
    public string? Title { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Session tracking (useful for analytics)
    public string? SessionMetadata { get; set; } // JSON: IP, user agent, referrer, etc.
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public List<ChatMessage> Messages { get; set; } = new();
    
    // Computed properties
    public int MessageCount => Messages.Count;
    public bool IsAnonymous => string.IsNullOrEmpty(UserId);
}
