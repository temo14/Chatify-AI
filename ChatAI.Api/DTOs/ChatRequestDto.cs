using ChatAI.Application.Models.Request;

namespace ChatAI.Api.DTOs;

public class ChatRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
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
