using ChatAI.Application.Models.Request;
using System.ComponentModel.DataAnnotations;

namespace ChatAI.Api.DTOs;

public class ChatRequestDto
{
    /// <summary>
    /// Optional user identifier (for future auth integration)
    /// </summary>
    [StringLength(100, ErrorMessage = "UserId must not exceed 100 characters")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// The user's message/prompt
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [StringLength(10000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 10000 characters")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID to continue an existing conversation. 
    /// Leave null/empty to start a new chat - server will create and return new sessionId
    /// </summary>
    [StringLength(100, ErrorMessage = "SessionId must not exceed 100 characters")]
    public string? SessionId { get; set;}
    
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
