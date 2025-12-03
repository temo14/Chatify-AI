namespace ChatAI.Domain.Enums;

/// <summary>
/// Represents the type of tool that can be called
/// </summary>
public enum ToolType
{
    /// <summary>
    /// Built-in application tool
    /// </summary>
    Internal,

    /// <summary>
    /// External API or service
    /// </summary>
    External,

    /// <summary>
    /// Function provided by AI model
    /// </summary>
    Function
}
