using ChatAI.Application.Configuration;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.AI;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using DomainChatMessage = ChatAI.Domain.Entities.ChatMessage;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Azure OpenAI client that gets tool definitions from ToolExecutor
/// </summary>
public class AzureOpenAIClient : IAIClient
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IToolExecutor _toolExecutor;
    private readonly ILogger<AzureOpenAIClient> _logger;
    private readonly AzureOpenAIOptions _options;

    public AzureOpenAIClient(
        ChatClient chatClient,
        EmbeddingClient embeddingClient,
        IToolExecutor toolExecutor,
        ILogger<AzureOpenAIClient> logger,
        IOptions<AzureOpenAIOptions> options)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AIResponse> GenerateResponseAsync(List<DomainChatMessage> messages, bool allowTools = true)
    {
        if (messages == null || !messages.Any())
        {
            throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
        }

        try
        {
            _logger.LogInformation("Generating AI response. Messages: {Count}, AllowTools: {AllowTools}", 
                messages.Count, allowTools);

            // Convert domain messages to OpenAI format
            var openAIMessages = ConvertToOpenAIMessages(messages);

            // Build options with tools if enabled
            var options = BuildChatOptions(allowTools);

            // Call Azure OpenAI
            var response = await _chatClient.CompleteChatAsync(openAIMessages, options);

            if (response?.Value == null)
            {
                throw new AIServiceException("Azure OpenAI returned null response");
            }

            var result = MapToAIResponse(response.Value, allowTools);

            _logger.LogInformation("AI response generated. HasToolCall: {HasToolCall}, Model: {Model}", 
                result.ToolCall != null, result.Model);

            return result;
        }
        catch (Exception ex) when (ex is not AIServiceException)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI");
            throw new AIServiceException("Failed to generate AI response", ex);
        }
    }

    /// <summary>
    /// Build chat completion options with tools from ToolExecutor
    /// </summary>
    private ChatCompletionOptions BuildChatOptions(bool allowTools)
    {
        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxTokens,
            Temperature = (float)_options.Temperature
        };

        if (allowTools)
        {
            // Get tool definitions from ToolExecutor (SINGLE SOURCE OF TRUTH)
            var toolDefinitions = _toolExecutor.GetAllToolDefinitions();
            
            foreach (var toolDef in toolDefinitions)
            {
                var chatTool = ChatTool.CreateFunctionTool(
                    functionName: toolDef.Name,
                    functionDescription: toolDef.Description,
                    functionParameters: BinaryData.FromString(toolDef.ParametersJsonSchema)
                );
                
                options.Tools.Add(chatTool);
            }
            
            _logger.LogDebug("Registered {Count} tools for AI", options.Tools.Count);
        }

        return options;
    }

    /// <summary>
    /// Map OpenAI response to domain AIResponse
    /// </summary>
    private AIResponse MapToAIResponse(ChatCompletion completion, bool allowTools)
    {
        var content = completion.Content?.FirstOrDefault()?.Text ?? string.Empty;
        AIToolCall? toolCall = null;

        if (allowTools && completion.ToolCalls?.Any() == true)
        {
            var firstToolCall = completion.ToolCalls.First();
            toolCall = new AIToolCall
            {
                Name = firstToolCall.FunctionName,
                Arguments = firstToolCall.FunctionArguments.ToString(),
                Id = firstToolCall.Id
            };

            _logger.LogDebug("Tool call requested: {ToolName}", toolCall.Name);
        }

        return new AIResponse
        {
            Content = content,
            ToolCall = toolCall,
            Model = completion.Model ?? "unknown",
            TokensUsed = completion.Usage?.TotalTokenCount,
            FinishReason = completion.FinishReason.ToString()
        };
    }

    /// <summary>
    /// Converts YOUR ChatMessage entities to OpenAI SDK ChatMessage format
    /// This is the KEY conversion that fixes the naming conflict
    /// </summary>
    private List<OpenAIChatMessage> ConvertToOpenAIMessages(List<DomainChatMessage> messages)
    {
        var openAIMessages = new List<OpenAIChatMessage>();

        foreach (var msg in messages)
        {
            OpenAIChatMessage openAIMessage = msg.Role switch
            {
                MessageRole.System => new SystemChatMessage(msg.Content),
                MessageRole.User => new UserChatMessage(msg.Content),
                MessageRole.Assistant => new AssistantChatMessage(msg.Content),
                MessageRole.Tool => new ToolChatMessage(msg.ToolName ?? "unknown", msg.Content),
                _ => new UserChatMessage(msg.Content)
            };

            openAIMessages.Add(openAIMessage);
        }

        return openAIMessages;
    }

    /// <summary>
    /// Generate embeddings for RAG/semantic search
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        try
        {
            _logger.LogDebug("Generating embedding for input length: {Length}", input.Length);
            
            var response = await _embeddingClient.GenerateEmbeddingAsync(input, cancellationToken: ct);
            var embedding = response.Value.ToFloats().ToArray();
            
            _logger.LogDebug("Embedding generated with {Dimensions} dimensions", embedding.Length);
            
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw new AIServiceException("Failed to generate embedding", ex);
        }
    }
}
