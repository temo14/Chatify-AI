using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a single message in a chat conversation
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    // Tool call information (if this message involved a tool)
    public bool IsToolCall { get; set; }
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
    public string? ToolResult { get; set; }
    
    // Optional: For future RAG/embedding support
    public string? EmbeddingReference { get; set; }
}
