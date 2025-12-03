using ChatAI.Domain.Entities;

namespace ChatAI.Api.DTOs;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;  // "User" / "Assistant" / "System" / "Tool"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public static ChatMessageDto FromDomain(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Role = message.Role.ToString(),
            Content = message.Content,
            Timestamp = message.Timestamp
        };
    }
}
