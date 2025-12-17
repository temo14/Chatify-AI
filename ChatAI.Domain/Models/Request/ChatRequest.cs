namespace ChatAI.Domain.Models.Request;

/// <summary>
/// Chat request model for application layer
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Optional user identifier (for future auth integration)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// The user's message/prompt
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID to continue an existing conversation. 
    /// Null/empty = start new chat
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Whether to enable tool/function calling (calculator, time, etc.)
    /// </summary>
    public bool UseTools { get; set; } = true;
}
