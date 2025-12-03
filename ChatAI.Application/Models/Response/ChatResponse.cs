using ChatAI.Domain.Entities;

namespace ChatAI.Application.Models.Response;

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool ToolCalled { get; set; }
    public ToolCallInfo? ToolCall { get; set; }
    public List<ChatMessage> ContextUsed { get; set; } = new();
}

public class ToolCallInfo
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}
