namespace ChatAI.Domain.Models;

/// <summary>
/// AI chat settings from database configuration
/// </summary>
public class AIChatSettings
{
    public string SystemPrompt { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public double FrequencyPenalty { get; set; }
    public double PresencePenalty { get; set; }
    public string ModelName { get; set; } = string.Empty;
}
