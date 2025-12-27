using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Entities;

/// <summary>
/// Stores user feedback (thumbs up/down) on AI responses
/// </summary>
public class MessageFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Tenant (customer) that owns this feedback
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Reference to the ChatMessage that was rated
    /// </summary>
    public Guid MessageId { get; set; }
    
    /// <summary>
    /// User who provided feedback
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Session where the message occurred
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Rating: 1 = thumbs up, -1 = thumbs down
    /// </summary>
    public int Rating { get; set; }
    
    /// <summary>
    /// Optional comment explaining the rating
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// When feedback was submitted
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Category of the issue (for negative feedback)
    /// </summary>
    public FeedbackCategory? Category { get; set; }
    
    /// <summary>
    /// IP address of the user (for analytics)
    /// </summary>
    public string? IpAddress { get; set; }
}
