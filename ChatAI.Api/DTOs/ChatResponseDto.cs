using ChatAI.Domain.Models.Response;
using ChatAI.Domain.Entities;

namespace ChatAI.Api.DTOs;

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool ToolCalled { get; set; }
    public ToolCallInfoDto? ToolCall { get; set; }
    public List<ChatMessageDto> ContextUsed { get; set; } = new();

    public static ChatResponseDto FromDomain(ChatResponse response)
    {
        return new ChatResponseDto
        {
            Reply = response.Reply,
            SessionId = response.SessionId,
            ToolCalled = response.ToolCalled,
            ToolCall = response.ToolCall != null ? ToolCallInfoDto.FromDomain(response.ToolCall) : null,
            ContextUsed = response.ContextUsed.Select(ChatMessageDto.FromDomain).ToList()
        };
    }
}
