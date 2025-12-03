namespace ChatAI.Application.Models.Tools;

/// <summary>
/// Represents a tool/function that can be called by AI
/// </summary>
public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersJsonSchema { get; set; } = string.Empty;
}
