namespace ChatAI.Domain.Enums;

/// <summary>
/// Represents the role of a message in a conversation
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the end user
    /// </summary>
    User,

    /// <summary>
    /// Message from the AI assistant
    /// </summary>
    Assistant,

    /// <summary>
    /// System instruction or context message
    /// </summary>
    System,

    /// <summary>
    /// Response from a tool/function call
    /// </summary>
    Tool
}
