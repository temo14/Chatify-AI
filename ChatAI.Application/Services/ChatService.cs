using ChatAI.Application.Configuration;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatAI.Application.Services;

/// <summary>
/// Orchestrates chat interactions with database persistence, RAG, logging, and tool support
/// </summary>
public class ChatService : IChatService
{
    private readonly IAIClient _aiClient;
    private readonly IToolExecutor _toolExecutor;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatOptions _options;

    public ChatService(
        IAIClient aiClient,
        IToolExecutor toolExecutor,
        IChatSessionRepository sessionRepository,
        IKnowledgeRepository knowledgeRepository,
        ILogger<ChatService> logger,
        IOptions<ChatOptions> options)
    {
        _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(request));
        }

        _logger.LogInformation("Processing chat request for user {UserId}, session {SessionId}", 
            request.UserId, request.SessionId);

        try
        {
            // 1. Get or create chat session
            var session = await GetOrCreateSessionAsync(request.UserId, request.SessionId);

            // 2. RAG: Search knowledge base for relevant context
            var relevantKnowledge = await SearchKnowledgeBaseAsync(request.Message);

            // 3. Build conversation history (load from database + add current message)
            var messages = await BuildConversationHistoryAsync(request, session.Id, relevantKnowledge);

            // 4. Execute tool calling loop
            var response = await ExecuteConversationLoopAsync(request, messages);

            // 5. Save conversation to database
            await SaveConversationAsync(session.Id, request.UserId, messages);

            // 6. Update session metadata
            await UpdateSessionAsync(session);

            _logger.LogInformation("Chat request completed. ToolCalled: {ToolCalled}", response.ToolCalled);

            return response;
        }
        catch (Exception ex) when (ex is not AIException)
        {
            _logger.LogError(ex, "Unexpected error processing chat request");
            throw new AIException("Failed to process chat request", ex);
        }
    }

    /// <summary>
    /// Execute conversation loop with tool calling support
    /// </summary>
    private async Task<ChatResponse> ExecuteConversationLoopAsync(ChatRequest request, List<ChatMessage> messages)
    {
        int toolCallCount = 0;
        bool toolWasCalled = false;
        ToolCallInfo? lastToolCall = null;

        while (toolCallCount < _options.MaxToolCalls)
        {
            // Call AI
            var aiResponse = await _aiClient.GenerateResponseAsync(
                messages, 
                request.UseTools && toolCallCount < _options.MaxToolCalls);

            // If AI doesn't want a tool, we're done
            if (aiResponse.ToolCall == null)
            {
                return new ChatResponse
                {
                    Reply = aiResponse.Content,
                    SessionId = request.SessionId,
                    ToolCalled = toolWasCalled,
                    ToolCall = lastToolCall,
                    ContextUsed = messages
                };
            }

            // AI requested a tool
            toolWasCalled = true;
            toolCallCount++;

            _logger.LogInformation("Executing tool {ToolName} (attempt {Attempt}/{Max})", 
                aiResponse.ToolCall.Name, toolCallCount, _options.MaxToolCalls);

            // Execute the tool
            var toolResult = await ExecuteToolWithHandlingAsync(
                aiResponse.ToolCall.Name, 
                aiResponse.ToolCall.Arguments);

            lastToolCall = new ToolCallInfo
            {
                Name = aiResponse.ToolCall.Name,
                Arguments = aiResponse.ToolCall.Arguments,
                Result = toolResult
            };

            // Add assistant's tool call request to conversation
            messages.Add(CreateToolCallMessage(request, aiResponse.ToolCall));

            // Add tool result to conversation
            messages.Add(CreateToolResultMessage(request, aiResponse.ToolCall.Name, toolResult));
        }

        // Safety: Max tool calls reached
        _logger.LogWarning("Maximum tool calls ({Max}) reached for session {SessionId}", 
            _options.MaxToolCalls, request.SessionId);

        return new ChatResponse
        {
            Reply = "I encountered an issue processing your request with multiple tools.",
            SessionId = request.SessionId,
            ToolCalled = toolWasCalled,
            ToolCall = lastToolCall,
            ContextUsed = messages
        };
    }

    /// <summary>
    /// Execute tool with error handling
    /// </summary>
    private async Task<string> ExecuteToolWithHandlingAsync(string toolName, string arguments)
    {
        try
        {
            if (!_toolExecutor.IsToolRegistered(toolName))
            {
                var error = $"Tool '{toolName}' is not registered";
                _logger.LogWarning(error);
                return $"Error: {error}";
            }

            return await _toolExecutor.ExecuteAsync(toolName, arguments);
        }
        catch (ToolExecutionException ex)
        {
            _logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
            return $"Error executing tool: {ex.Message}";
        }
    }

    /// <summary>
    /// Create assistant message for tool call
    /// </summary>
    private ChatMessage CreateToolCallMessage(ChatRequest request, Application.Models.AI.AIToolCall toolCall)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            UserId = request.UserId,
            Role = MessageRole.Assistant,
            Content = string.Empty,
            IsToolCall = true,
            ToolName = toolCall.Name,
            ToolArguments = toolCall.Arguments,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create tool result message
    /// </summary>
    private ChatMessage CreateToolResultMessage(ChatRequest request, string toolName, string result)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            UserId = request.UserId,
            Role = MessageRole.Tool,
            Content = result,
            ToolName = toolName,
            ToolResult = result,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get existing session or create new one with database persistence
    /// </summary>
    private async Task<ChatSession> GetOrCreateSessionAsync(string userId, string? sessionId)
    {
        ChatSession? session = null;

        // Try to load existing session
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = await _sessionRepository.GetByIdAsync(sessionId);
        }

        // Create new session if not found
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

    /// <summary>
    /// RAG: Search knowledge base for relevant documents to inject as context
    /// This is the core of RAG - finding relevant information before AI generates response
    /// </summary>
    private async Task<List<KnowledgeDocument>> SearchKnowledgeBaseAsync(string query)
    {
        try
        {
            var documents = await _knowledgeRepository.SearchAsync(query, topK: 3);
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

    /// <summary>
    /// Build conversation history: system prompt + RAG context + previous messages + current message
    /// </summary>
    private async Task<List<ChatMessage>> BuildConversationHistoryAsync(
        ChatRequest request, 
        string sessionId,
        List<KnowledgeDocument> knowledgeDocs)
    {
        var messages = new List<ChatMessage>();

        // 1. System prompt with RAG context injected
        var systemPrompt = BuildSystemPromptWithRAG(knowledgeDocs);
        messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = request.UserId,
            Role = MessageRole.System,
            Content = systemPrompt,
            Timestamp = DateTime.UtcNow
        });

        // 2. Load previous conversation history from database
        var previousMessages = await _sessionRepository.GetSessionMessagesAsync(sessionId);
        var recentMessages = previousMessages
            .OrderByDescending(m => m.Timestamp)
            .Take(_options.MaxConversationHistory)
            .Reverse()
            .ToList();
        
        if (recentMessages.Any())
        {
            messages.AddRange(recentMessages);
            _logger.LogDebug("Loaded {Count} previous messages from session {SessionId}", recentMessages.Count, sessionId);
        }

        // 3. Add current user message
        messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = request.UserId,
            Role = MessageRole.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        return messages;
    }

    /// <summary>
    /// Build system prompt with RAG context
    /// </summary>
    private string BuildSystemPromptWithRAG(List<KnowledgeDocument> knowledgeDocs)
    {
        var prompt = _options.DefaultSystemPrompt;

        if (knowledgeDocs.Any())
        {
            prompt += "\n\n**Relevant Knowledge Base:**\n";
            foreach (var doc in knowledgeDocs)
            {
                prompt += $"\n[{doc.Title}]\n{doc.Content}\n";
            }
            prompt += "\nUse this information when answering the user's question.";
            
            _logger.LogDebug("Injected {Count} knowledge documents into system prompt", knowledgeDocs.Count);
        }

        return prompt;
    }

    /// <summary>
    /// Save conversation messages to database
    /// </summary>
    private async Task SaveConversationAsync(string sessionId, string userId, List<ChatMessage> messages)
    {
        try
        {
            // Save only new messages (User and Assistant roles)
            var newMessages = messages
                .Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant)
                .Where(m => m.SessionId == sessionId)
                .ToList();

            if (newMessages.Any())
            {
                await _sessionRepository.AddMessagesAsync(newMessages);
                _logger.LogInformation("Saved {Count} messages to session {SessionId}", newMessages.Count, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save conversation to database");
            // Don't throw - conversation succeeded, saving is secondary
        }
    }

    /// <summary>
    /// Update session metadata
    /// </summary>
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
            // Don't throw
        }
    }

    /// <summary>
    /// Builds conversation history with system prompt
    /// DEPRECATED: Use BuildConversationHistoryAsync instead
    /// </summary>
    private List<ChatMessage> BuildConversationHistory(ChatRequest request)
    {
        var messages = new List<ChatMessage>();

        // Add system prompt
        messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            UserId = request.UserId,
            Role = MessageRole.System,
            Content = _options.DefaultSystemPrompt,
            Timestamp = DateTime.UtcNow
        });

        // TODO: Load previous messages from database for this session
        // var previousMessages = await _chatRepository.GetSessionMessages(request.SessionId, _options.MaxConversationHistory);
        // messages.AddRange(previousMessages);

        // Add current user message
        messages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            UserId = request.UserId,
            Role = MessageRole.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        return messages;
    }
}
