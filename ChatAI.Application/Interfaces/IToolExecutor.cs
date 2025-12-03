using ChatAI.Application.Models.Tools;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Registry and executor for AI tools/functions
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Execute a tool by name with given arguments
    /// </summary>
    Task<string> ExecuteAsync(string toolName, string arguments, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    bool IsToolRegistered(string toolName);
    
    /// <summary>
    /// Get all registered tool definitions
    /// </summary>
    IEnumerable<ToolDefinition> GetAllToolDefinitions();
}
