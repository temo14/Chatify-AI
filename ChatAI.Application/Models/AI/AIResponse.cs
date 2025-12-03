namespace ChatAI.Application.Models.AI;

/// <summary>
/// Response from AI provider
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public AIToolCall? ToolCall { get; set; }
    public string? Model { get; set; }
    public int? TokensUsed { get; set; }
    public string? FinishReason { get; set; }
}

/// <summary>
/// Represents a tool call requested by the AI
/// </summary>
public class AIToolCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string? Id { get; set; }
}
