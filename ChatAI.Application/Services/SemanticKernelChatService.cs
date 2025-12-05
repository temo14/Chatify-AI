using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace ChatAI.Application.Services;

/// <summary>
/// Advanced chat service using Semantic Kernel for orchestration, plugins, and planning
/// This is an alternative implementation to ChatService that uses Semantic Kernel's
/// advanced features like auto-function calling and plugin composition.
/// </summary>
public class SemanticKernelChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ILogger<SemanticKernelChatService> _logger;
    private readonly ChatOptions _chatOptions;

    public SemanticKernelChatService(
        Kernel kernel,
        IChatSessionRepository sessionRepository,
        ILogger<SemanticKernelChatService> logger,
        IOptions<ChatOptions> chatOptions)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _sessionRepository = sessionRepository;
        _logger = logger;
        _chatOptions = chatOptions.Value;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest request)
    {
        _logger.LogInformation("🧠 Semantic Kernel processing request for user {UserId}", request.UserId);

        // Get or create session
        var session = await GetOrCreateSessionAsync(request);

        // Load conversation history
        var history = await LoadHistoryAsync(session.Id);
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

        // Add system message
        chatHistory.AddSystemMessage(_chatOptions.DefaultSystemPrompt);

        // Add historical messages
        foreach (var msg in history)
        {
            if (msg.Role == MessageRole.User)
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == MessageRole.Assistant)
                chatHistory.AddAssistantMessage(msg.Content);
        }

        // Add current user message
        chatHistory.AddUserMessage(request.Message);

        // Save user message
        await SaveMessageAsync(session.Id, request.Message, MessageRole.User);

        // Generate response using Semantic Kernel
        // Note: Semantic Kernel 1.28.0 automatically invokes kernel functions
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 800,
            TopP = 0.9,
            FrequencyPenalty = 0.0,
            PresencePenalty = 0.0
        };

        try
        {
            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                settings,
                _kernel);

            var reply = result.Content ?? "I couldn't generate a response.";

            // Save assistant response
            await SaveMessageAsync(session.Id, reply, MessageRole.Assistant);

            // Check if tools were called
            var toolCalled = result.Metadata?.ContainsKey("ToolCalls") == true;
            ToolCallInfo? toolInfo = null;

            if (toolCalled && result.Metadata?.TryGetValue("ToolCalls", out var toolCalls) == true)
            {
                // Extract tool call information
                var calls = toolCalls as IEnumerable<object> ?? Array.Empty<object>();
                var firstCall = calls.FirstOrDefault();
                
                if (firstCall != null)
                {
                    toolInfo = new ToolCallInfo
                    {
                        Name = "KernelFunction",
                        Arguments = firstCall.ToString() ?? "",
                        Result = "Executed via Semantic Kernel"
                    };
                }
            }

            _logger.LogInformation("✅ Semantic Kernel response generated (length: {Length}, tools called: {ToolCalled})", 
                reply.Length, toolCalled);

            return new ChatResponse
            {
                Reply = reply,
                SessionId = session.Id,
                ToolCalled = toolCalled,
                ToolCall = toolInfo,
                ContextUsed = new List<ChatMessage>() // SK doesn't use RAG in this simplified version
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating Semantic Kernel response");
            throw;
        }
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(ChatRequest request)
    {
        if (!string.IsNullOrEmpty(request.SessionId))
        {
            var existing = await _sessionRepository.GetByIdAsync(request.SessionId);
            if (existing != null)
            {
                existing.LastActivityAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(existing);
                return existing;
            }
        }

        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            Title = request.Message.Length > 50 
                ? request.Message.Substring(0, 50) + "..." 
                : request.Message,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        return await _sessionRepository.AddAsync(session);
    }

    private async Task<List<ChatMessage>> LoadHistoryAsync(string sessionId)
    {
        var messages = await _sessionRepository.GetSessionMessagesAsync(sessionId);
        return messages
            .OrderBy(m => m.Timestamp)
            .TakeLast(_chatOptions.MaxConversationHistory)
            .ToList();
    }

    private async Task SaveMessageAsync(string sessionId, string content, MessageRole role)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Content = content,
            Role = role,
            Timestamp = DateTime.UtcNow
        };

        await _sessionRepository.AddMessageAsync(message);
    }
}
