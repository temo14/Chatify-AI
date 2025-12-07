namespace ChatAI.Application.Services;

/// <summary>
/// Scoped context for tracking chat session information across the request lifecycle.
/// Used to correlate tool calls with their originating session.
/// </summary>
public class ChatContext
{
    /// <summary>
    /// Current chat session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Current user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Get a formatted context string for logging
    /// </summary>
    public string GetContextInfo() => 
        $"Session={SessionId ?? "new"}, User={UserId ?? "anonymous"}";
}
