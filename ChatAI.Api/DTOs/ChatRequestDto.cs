using ChatAI.Application.Models.Request;

namespace ChatAI.Api.DTOs;

public class ChatRequestDto
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
    /// Leave null/empty to start a new chat - server will create and return new sessionId
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Whether to enable tool/function calling (calculator, time, etc.)
    /// </summary>
    public bool UseTools { get; set; } = true;

    public ChatRequest ToDomain()
    {
        return new ChatRequest
        {
            UserId = UserId,
            Message = Message,
            SessionId = SessionId,
            UseTools = UseTools
        };
    }
}
