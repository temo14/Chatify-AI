using ChatAI.Application.Configuration;
using ChatAI.Application.Services;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Request;
using ChatAI.Domain.Models.Response;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly ChatContext _chatContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SemanticKernelChatService> _logger;
    private readonly ChatOptions _chatOptions;

    public SemanticKernelChatService(
        Kernel kernel,
        IChatSessionRepository sessionRepository,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository,
        ChatContext chatContext,
        IServiceProvider serviceProvider,
        ILogger<SemanticKernelChatService> logger,
        IOptions<ChatOptions> chatOptions)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _sessionRepository = sessionRepository;
        _tenantContext = tenantContext;
        _tenantRepository = tenantRepository;
        _chatContext = chatContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chatOptions = chatOptions.Value;
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

        // Load AI settings from tenant settings
        var tenant = await _tenantRepository.GetByIdAsync(_tenantContext.RequiredTenantId);
        var settings = tenant?.Settings;
        
        if (settings == null)
        {
            throw new InvalidOperationException("Tenant settings not found");
        }

        // Load conversation history with retention filter
        var history = await LoadHistoryAsync(session.Id, settings.ChatHistoryRetentionDays);
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

        // Add system message from tenant settings or generate default with tenant branding
        var systemPrompt = settings.SystemPrompt ?? (tenant != null ? GenerateDefaultSystemPrompt(tenant) : "You are a helpful AI assistant.");
        chatHistory.AddSystemMessage(systemPrompt);

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

        // Conditionally register plugins based on tenant feature toggles
        RegisterPluginsForRequest(settings);

        // Configure AI execution settings with automatic tool calling from tenant settings
        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TopP = 0.95,
            FrequencyPenalty = 0.3,
            PresencePenalty = 0.2,
            // CRITICAL: Respect tenant's EnableTools setting
            FunctionChoiceBehavior = settings.EnableTools 
                ? FunctionChoiceBehavior.Auto() 
                : null  // Disable function calling when tools are disabled
        };

        _logger.LogDebug("[{Context}] Invoking chat completion (tools enabled: {ToolsEnabled}, temp: {Temp}, max tokens: {MaxTokens})", 
            _chatContext.GetContextInfo(), settings.EnableTools, executionSettings.Temperature, executionSettings.MaxTokens);

        var result = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
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

    private async Task<List<ChatMessage>> LoadHistoryAsync(string sessionId, int retentionDays)
    {
        var messages = await _sessionRepository.GetSessionMessagesAsync(sessionId);
        
        // Filter by retention period (0 = no history, > 0 = keep X days)
        if (retentionDays <= 0)
        {
            return new List<ChatMessage>();
        }
        
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        return messages
            .Where(m => m.Timestamp >= cutoffDate)
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

    /// <summary>
    /// Generate a default system prompt with tenant branding
    /// </summary>
    private string GenerateDefaultSystemPrompt(Tenant tenant)
    {
        var companyName = !string.IsNullOrWhiteSpace(tenant.Name) ? tenant.Name : "our company";
        var contactEmail = !string.IsNullOrWhiteSpace(tenant.Email) ? tenant.Email : "your administrator";

        return $"You are the AI assistant for {companyName}. " +
               $"You help customers with questions about our services, products, and policies. " +
               $"Be helpful, professional, and concise in your responses. " +
               $"If you cannot answer a question, be honest about it and suggest contacting {contactEmail} for assistance.";
    }

    /// <summary>
    /// Register plugins conditionally based on tenant feature toggles
    /// This ensures AI only sees and advertises features that are actually enabled
    /// </summary>
    private void RegisterPluginsForRequest(TenantSettings settings)
    {
        // Clear any existing plugins from previous requests (kernel is scoped)
        _kernel.Plugins.Clear();

        // Only register plugins if tools are enabled for this tenant
        if (!settings.EnableTools)
        {
            _logger.LogDebug("[{Context}] Tools disabled - no plugins registered", _chatContext.GetContextInfo());
            return;
        }

        // Always register KnowledgePlugin when tools are enabled
        var knowledgePlugin = _serviceProvider.GetRequiredService<ChatAI.Application.Plugins.KnowledgePlugin>();
        _kernel.Plugins.AddFromObject(knowledgePlugin, "KnowledgePlugin");
        _logger.LogDebug("[{Context}] Registered KnowledgePlugin", _chatContext.GetContextInfo());

        // Only register EmailPlugin if email support is enabled
        if (settings.EnableEmailSupport)
        {
            var emailPlugin = _serviceProvider.GetRequiredService<ChatAI.Application.Plugins.EmailPlugin>();
            _kernel.Plugins.AddFromObject(emailPlugin, "EmailPlugin");
            _logger.LogDebug("[{Context}] Registered EmailPlugin (email support enabled)", _chatContext.GetContextInfo());
        }
        else
        {
            _logger.LogDebug("[{Context}] EmailPlugin NOT registered (email support disabled)", _chatContext.GetContextInfo());
        }
    }
}
