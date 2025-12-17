using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;

using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Chat.GetConversationHistory;

/// <summary>
/// Handler for GetConversationHistoryQuery - retrieves chat history
/// </summary>
public class GetConversationHistoryQueryHandler : IRequestHandler<GetConversationHistoryQuery, List<ChatMessage>>
{
    private readonly IChatSessionRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetConversationHistoryQueryHandler> _logger;

    public GetConversationHistoryQueryHandler(
        IChatSessionRepository repository,
        ICacheService cache,
        ILogger<GetConversationHistoryQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<ChatMessage>> Handle(GetConversationHistoryQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Retrieving conversation history for session {SessionId}", query.SessionId);

        var cacheKey = $"conversation_history:{query.SessionId}";
        
        var messages = await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _repository.GetSessionMessagesAsync(query.SessionId),
            TimeSpan.FromMinutes(60));

        var result = messages
            .OrderByDescending(m => m.Timestamp)
            .Take(query.MaxMessages)
            .Reverse()
            .ToList();

        _logger.LogInformation("✅ Retrieved {Count} messages from session {SessionId}", result.Count, query.SessionId);

        return result;
    }
}
