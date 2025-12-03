namespace ChatAI.Application.Configuration;

/// <summary>
/// Chat service configuration
/// </summary>
public class ChatOptions
{
    public const string SectionName = "Chat";

    public int MaxToolCalls { get; set; } = 5;
    public int MaxConversationHistory { get; set; } = 20;
    public string DefaultSystemPrompt { get; set; } = "You are a helpful AI assistant.";
}
