using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for GetUserSessionsQuery - retrieves user's chat sessions
/// </summary>
public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, List<ChatSession>>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<GetUserSessionsQueryHandler> _logger;

    public GetUserSessionsQueryHandler(
        IChatSessionRepository repository,
        ILogger<GetUserSessionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ChatSession>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📋 Retrieving sessions for user {UserId}", query.UserId);

        var sessions = await _repository.GetUserSessionsAsync(query.UserId);
        var sessionList = sessions.ToList();

        if (query.OnlyActive)
        {
            sessionList = sessionList.Where(s => s.IsActive).ToList();
        }

        _logger.LogInformation("✅ Retrieved {Count} sessions for user {UserId}", sessionList.Count, query.UserId);

        return sessionList;
    }
}
