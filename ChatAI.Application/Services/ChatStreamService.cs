using ChatAI.Application.Configuration;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;
using ChatAI.Application.Services;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Runtime.CompilerServices;

namespace ChatAI.Application.Services;

/// <summary>
/// Chat service with streaming support using Semantic Kernel
/// </summary>
public class ChatStreamService : IChatStreamService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChatStreamService> _logger;
    private readonly ChatOptions _options;
    private readonly CacheOptions _cacheOptions;
    private readonly IConfigurationService _configService;

    public ChatStreamService(
        Kernel kernel,
        IChatSessionRepository sessionRepository,
        IKnowledgeRepository knowledgeRepository,
        ICacheService cacheService,
        ILogger<ChatStreamService> logger,
        IOptions<ChatOptions> options,
        IOptions<CacheOptions> cacheOptions,
        IConfigurationService configService)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    public async IAsyncEnumerable<StreamChunk> HandleStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(request));
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 Starting streaming chat for user {UserId}, session {SessionId}", 
            request.UserId, request.SessionId);

        // Delegate to helper method that can yield
        await foreach (var chunk in StreamInternalAsync(request, startTime, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<StreamChunk> StreamInternalAsync(
        ChatRequest request,
        DateTime startTime,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatSession session;
        var sequenceNumber = 0;
        var completeResponse = new List<string>();

        // 1. Get or create session
        session = await GetOrCreateSessionAsync(request.UserId ?? "anonymous", request.SessionId);
        
        if (session == null)
        {
            throw new InvalidOperationException("Failed to create or retrieve chat session");
        }

        // 2. RAG: Search knowledge base
        var relevantKnowledge = await SearchKnowledgeBaseAsync(request.Message);

        // 3. Build conversation history
        var chatHistory = await BuildConversationHistoryAsync(request, session.Id, relevantKnowledge);

        // 4. Load AI settings from database configuration (cached)
        var cacheKey = CacheKeyBuilder.AISettings();
        var aiSettings = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () => await _configService.GetAISettingsAsync(cancellationToken).ConfigureAwait(false),
            TimeSpan.FromMinutes(10)).ConfigureAwait(false);

        // 5. Stream AI response using Semantic Kernel with dynamic configuration
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            Temperature = aiSettings.Temperature,
            MaxTokens = aiSettings.MaxTokens,
            TopP = aiSettings.TopP,
            FrequencyPenalty = aiSettings.FrequencyPenalty,
            PresencePenalty = aiSettings.PresencePenalty
        };

        _logger.LogDebug("Streaming with AI settings: Temp={Temp}, MaxTokens={MaxTokens}", 
            settings.Temperature, settings.MaxTokens);

        await foreach (var contentChunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory, 
            settings,
            _kernel, 
            cancellationToken).WithCancellation(cancellationToken))
        {
            var content = contentChunk.Content ?? string.Empty;
            if (!string.IsNullOrEmpty(content))
            {
                completeResponse.Add(content);

                yield return new StreamChunk
                {
                    SessionId = session.Id,
                    Content = content,
                    IsComplete = false,
                    SequenceNumber = ++sequenceNumber,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // 5. Save conversation to database
        var fullResponse = string.Join("", completeResponse);
        await SaveStreamedConversationAsync(session.Id, request.UserId ?? "anonymous", request.Message, fullResponse);

        // 6. Update session
        await UpdateSessionAsync(session);

        // 7. Send final chunk
        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation("✅ Streaming completed in {DurationMs}ms. Chunks: {ChunkCount}, SessionId: {SessionId}", 
            duration.TotalMilliseconds, sequenceNumber, session.Id);

        yield return new StreamChunk
        {
            SessionId = session.Id,
            Content = string.Empty,
            IsComplete = true,
            SequenceNumber = ++sequenceNumber,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(string userId, string? sessionId)
    {
        ChatSession? session = null;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = await _sessionRepository.GetByIdAsync(sessionId);
        }

        if (session == null)
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true
            };
            
            session = await _sessionRepository.AddAsync(session);
            _logger.LogInformation("Created new session {SessionId} for user {UserId}", session.Id, userId);
        }

        return session;
    }

    private async Task<List<KnowledgeDocument>> SearchKnowledgeBaseAsync(string query)
    {
        try
        {
            var documents = await _knowledgeRepository.SearchAsync(query, topK: _options.RagTopK);
            var results = documents.ToList();
            
            _logger.LogInformation("RAG: Found {Count} relevant knowledge documents", results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search knowledge base, continuing without RAG");
            return new List<KnowledgeDocument>();
        }
    }

    private async Task<Microsoft.SemanticKernel.ChatCompletion.ChatHistory> BuildConversationHistoryAsync(
        ChatRequest request, 
        string sessionId,
        List<KnowledgeDocument> knowledgeDocs)
    {
        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

        // Load AI settings to get system prompt from database (cached)
        var cacheKey = CacheKeyBuilder.AISettings();
        var aiSettings = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () => await _configService.GetAISettingsAsync().ConfigureAwait(false),
            TimeSpan.FromMinutes(10)).ConfigureAwait(false);
        
        // System prompt with RAG
        var systemPrompt = BuildSystemPromptWithRAG(knowledgeDocs, aiSettings.SystemPrompt);
        chatHistory.AddSystemMessage(systemPrompt);

        // Load previous messages with pagination (only load what we need)
        var historyCacheKey = CacheKeyBuilder.ConversationHistory(sessionId);
        var recentMessages = await _cacheService.GetOrCreateAsync(
            historyCacheKey,
            async () => await _sessionRepository.GetSessionMessagesAsync(
                sessionId, 
                skip: 0, 
                take: _options.MaxConversationHistory).ConfigureAwait(false),
            TimeSpan.FromMinutes(_cacheOptions.ConversationExpirationMinutes)).ConfigureAwait(false);
        
        // Add historical messages (already sorted and limited by pagination)
        foreach (var msg in recentMessages)
        {
            if (msg.Role == MessageRole.User)
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == MessageRole.Assistant)
                chatHistory.AddAssistantMessage(msg.Content);
        }

        // Add current user message
        chatHistory.AddUserMessage(request.Message);

        return chatHistory;
    }

    private string BuildSystemPromptWithRAG(List<KnowledgeDocument> knowledgeDocs, string systemPrompt)
    {
        var prompt = systemPrompt;

        if (knowledgeDocs.Any())
        {
            prompt += "\n\n**Relevant Knowledge Base:**\n";
            foreach (var doc in knowledgeDocs)
            {
                prompt += $"\n[{doc.Title}]\n{doc.Content}\n";
            }
            prompt += "\nUse this information when answering the user's question.";
        }

        return prompt;
    }

    private async Task SaveStreamedConversationAsync(
        string sessionId, 
        string userId, 
        string userMessage, 
        string assistantResponse)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = userId,
                    Role = MessageRole.User,
                    Content = userMessage,
                    Timestamp = DateTime.UtcNow
                },
                new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    UserId = userId,
                    Role = MessageRole.Assistant,
                    Content = assistantResponse,
                    Timestamp = DateTime.UtcNow
                }
            };

            await _sessionRepository.AddMessagesAsync(messages).ConfigureAwait(false);
            
            // Invalidate cache
            var cacheKey = CacheKeyBuilder.ConversationHistory(sessionId);
            _cacheService.Remove(cacheKey);
            
            _logger.LogInformation("Saved streamed conversation to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save streamed conversation");
        }
    }

    private async Task UpdateSessionAsync(ChatSession session)
    {
        try
        {
            session.LastActivityAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session {SessionId}", session.Id);
        }
    }
}
