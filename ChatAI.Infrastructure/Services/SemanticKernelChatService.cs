using ChatAI.Application.Configuration;
using ChatAI.Application.Services;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Request;
using ChatAI.Domain.Models.Response;
using ChatAI.Domain.Models;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace ChatAI.Infrastructure.Services;

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
    private readonly ChatContext _chatContext;
    private readonly ILogger<SemanticKernelChatService> _logger;
    private readonly ChatOptions _chatOptions;
    private readonly IConfigurationService _configService;

    public SemanticKernelChatService(
        Kernel kernel,
        IChatSessionRepository sessionRepository,
        ChatContext chatContext,
        ILogger<SemanticKernelChatService> logger,
        IOptions<ChatOptions> chatOptions,
        IConfigurationService configService)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _sessionRepository = sessionRepository;
        _chatContext = chatContext;
        _logger = logger;
        _chatOptions = chatOptions.Value;
        _configService = configService;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest request)
    {
        // Get or create session
        var session = await GetOrCreateSessionAsync(request);

        // Set context for tool call logging
        _chatContext.SessionId = session.Id;
        _chatContext.UserId = request.UserId ?? "anonymous";
        _chatContext.RequestTimestamp = DateTime.UtcNow;

        _logger.LogInformation("🧠 [{Context}] Semantic Kernel processing request", _chatContext.GetContextInfo());

        // Load AI settings from database configuration
        var aiSettings = await _configService.GetAISettingsAsync();

        // Load conversation history
        var history = await LoadHistoryAsync(session.Id);
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

        // Add system message from configuration
        chatHistory.AddSystemMessage(aiSettings.SystemPrompt);

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

        // Configure AI execution settings with automatic tool calling from database config
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            Temperature = aiSettings.Temperature,
            MaxTokens = aiSettings.MaxTokens,
            TopP = aiSettings.TopP,
            FrequencyPenalty = aiSettings.FrequencyPenalty,
            PresencePenalty = aiSettings.PresencePenalty,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        _logger.LogDebug("[{Context}] Invoking chat completion (tools enabled: {ToolsEnabled}, temp: {Temp}, max tokens: {MaxTokens})", 
            _chatContext.GetContextInfo(), request.UseTools, settings.Temperature, settings.MaxTokens);

        var result = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel);

        var reply = result.Content ?? "I couldn't generate a response.";

        // Save assistant response
        await SaveMessageAsync(session.Id, reply, MessageRole.Assistant);

        _logger.LogInformation("✅ [{Context}] Response generated - Length: {Length} chars", 
            _chatContext.GetContextInfo(), reply.Length);

        // Tool calls are logged directly in plugins with full session context
        return new ChatResponse
        {
            Reply = reply,
            SessionId = session.Id,
            ToolCalled = false,  // Plugins handle their own logging
            ToolCall = null,
            ContextUsed = new List<ChatMessage>()
        };
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
