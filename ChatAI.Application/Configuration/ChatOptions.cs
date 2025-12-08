namespace ChatAI.Application.Configuration;

/// <summary>
/// Chat service configuration
/// </summary>
public class ChatOptions
{
    public const string SectionName = "Chat";

    /// <summary>
    /// Maximum conversation history to include in AI context
    /// </summary>
    public int MaxConversationHistory { get; set; } = 20;
    
    /// <summary>
    /// Maximum message length in characters
    /// </summary>
    public int MaxMessageLength { get; set; } = 10000;
    
    /// <summary>
    /// Minimum semantic similarity score threshold (0.0 to 1.0)
    /// Documents below this threshold won't be included in RAG context
    /// </summary>
    public double SearchScoreThreshold { get; set; } = 0.7;
    
    /// <summary>
    /// Number of relevant documents to retrieve for RAG
    /// </summary>
    public int RagTopK { get; set; } = 3;
}
