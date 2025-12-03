using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Tools;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Tools;

/// <summary>
/// Tool executor with centralized registry - SINGLE SOURCE OF TRUTH
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly ILogger<ToolExecutor> _logger;
    private readonly Dictionary<string, Func<string, Task<string>>> _toolImplementations;
    private readonly Dictionary<string, ToolDefinition> _toolDefinitions;

    public ToolExecutor(ILogger<ToolExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolImplementations = new Dictionary<string, Func<string, Task<string>>>(StringComparer.OrdinalIgnoreCase);
        _toolDefinitions = new Dictionary<string, ToolDefinition>(StringComparer.OrdinalIgnoreCase);
        
        // Register built-in tools
        RegisterBuiltInTools();
    }

    public async Task<string> ExecuteAsync(string toolName, string arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));
        }

        if (!_toolImplementations.ContainsKey(toolName))
        {
            throw new ToolExecutionException(toolName, $"Tool '{toolName}' not found");
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName} with args: {Arguments}", toolName, arguments);
            
            var result = await _toolImplementations[toolName](arguments);
            
            _logger.LogInformation("Tool {ToolName} executed successfully", toolName);
            
            return result;
        }
        catch (Exception ex) when (ex is not ToolExecutionException)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            throw new ToolExecutionException(toolName, $"Failed to execute tool: {ex.Message}", ex);
        }
    }

    public bool IsToolRegistered(string toolName)
    {
        return _toolImplementations.ContainsKey(toolName);
    }

    public IEnumerable<ToolDefinition> GetAllToolDefinitions()
    {
        return _toolDefinitions.Values;
    }

    /// <summary>
    /// Register a tool with both definition and implementation
    /// </summary>
    private void RegisterTool(
        string name, 
        string description, 
        string parametersJsonSchema,
        Func<string, Task<string>> implementation)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tool name cannot be empty", nameof(name));
        }

        // Store definition (for AI)
        _toolDefinitions[name] = new ToolDefinition
        {
            Name = name,
            Description = description,
            ParametersJsonSchema = parametersJsonSchema
        };

        // Store implementation (for execution)
        _toolImplementations[name] = implementation ?? throw new ArgumentNullException(nameof(implementation));

        _logger.LogInformation("Tool registered: {ToolName}", name);
    }

    /// <summary>
    /// Register all built-in tools - SINGLE PLACE TO DEFINE TOOLS
    /// </summary>
    private void RegisterBuiltInTools()
    {
        // Weather Tool
        RegisterTool(
            name: "get_weather",
            description: "Get the current weather for a specific city",
            parametersJsonSchema: """
            {
                "type": "object",
                "properties": {
                    "city": {
                        "type": "string",
                        "description": "The city name, e.g., London, Paris"
                    },
                    "unit": {
                        "type": "string",
                        "enum": ["celsius", "fahrenheit"],
                        "description": "Temperature unit"
                    }
                },
                "required": ["city"]
            }
            """,
            implementation: async (args) =>
            {
                await Task.Delay(50); // Simulate API call
                // TODO: Parse args JSON and call real weather API
                return $"Weather data for {args}: 15°C, Cloudy";
            }
        );

        // Web Search Tool
        RegisterTool(
            name: "search_web",
            description: "Search the internet for information",
            parametersJsonSchema: """
            {
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "The search query"
                    }
                },
                "required": ["query"]
            }
            """,
            implementation: async (args) =>
            {
                await Task.Delay(50); // Simulate API call
                // TODO: Parse args JSON and call real search API
                return $"Search results for '{args}': [Sample result 1, Sample result 2]";
            }
        );
    }
}
