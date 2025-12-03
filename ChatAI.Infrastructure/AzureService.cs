using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.AI;
using ChatAI.Domain.Enums;
using OpenAI.Chat;
using OpenAI.Embeddings;
using DomainChatMessage = ChatAI.Domain.Entities.ChatMessage;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Azure OpenAI implementation of IAIClient
/// Uses the official OpenAI .NET SDK with Azure endpoint
/// </summary>
public class AzureOpenAIClient : IAIClient
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIClient(ChatClient chatClient, EmbeddingClient embeddingClient)
    {
        _chatClient = chatClient;
        _embeddingClient = embeddingClient;
    }

    /// <summary>
    /// Generates AI response from conversation history
    /// </summary>
    public async Task<AIResponse> GenerateResponseAsync(List<DomainChatMessage> messages, bool allowTools = true)
    {
        // STEP 1: Convert YOUR domain messages to OpenAI format
        var openAIMessages = ConvertToOpenAIMessages(messages);

        // STEP 2: Define what tools are available (if enabled)
        ChatCompletionOptions? options = null;
        if (allowTools)
        {
            options = new ChatCompletionOptions
            {
                // YOU tell Azure OpenAI what tools exist!
                Tools = { GetWeatherTool(), SearchWebTool() }
                
                // You can add more tools here:
                // Tools = { Tool1(), Tool2(), Tool3() }
            };
        }

        // STEP 3: Call Azure OpenAI WITH the tool definitions
        var response = await _chatClient.CompleteChatAsync(openAIMessages, options);

        // STEP 4: Extract content from response
        var content = response.Value?.Content?.FirstOrDefault()?.Text ?? string.Empty;

        // STEP 5: Check if AI wants to call a tool
        // Azure OpenAI will ONLY return toolCalls if:
        // - You provided tools in step 2
        // - The AI decided it needs one of those tools
        var toolCalls = response.Value?.ToolCalls;
        AIToolCall? toolCall = null;

        if (toolCalls?.Any() == true && allowTools)
        {
            var firstToolCall = toolCalls.First();
            toolCall = new AIToolCall
            {
                Name = firstToolCall.FunctionName,     // e.g., "get_weather"
                Arguments = firstToolCall.FunctionArguments.ToString(), // e.g., {"city": "London"}
                Id = firstToolCall.Id
            };
        }

        // STEP 6: Return structured response
        return new AIResponse
        {
            Content = content,
            ToolCall = toolCall,  // Will be null if no tool needed
            Model = response.Value?.Model ?? "unknown",
            FinishReason = response.Value?.FinishReason.ToString()
        };
    }

    /// <summary>
    /// EXAMPLE: Define a "get weather" tool
    /// This tells Azure OpenAI: "You can call this function"
    /// </summary>
    private ChatTool GetWeatherTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "get_weather",
            functionDescription: "Get the current weather for a specific city",
            functionParameters: BinaryData.FromString("""
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
            """)
        );
    }

    /// <summary>
    /// EXAMPLE: Define a "search web" tool
    /// </summary>
    private ChatTool SearchWebTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "search_web",
            functionDescription: "Search the internet for information",
            functionParameters: BinaryData.FromString("""
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
            """)
        );
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
        var response = await _embeddingClient.GenerateEmbeddingAsync(input, cancellationToken: ct);
        return response.Value.ToFloats().ToArray();
    }
}
