using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for GetSessionQuery - retrieves detailed session information
/// </summary>
public class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, GetSessionResult>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<GetSessionQueryHandler> _logger;

    public GetSessionQueryHandler(
        IChatSessionRepository repository,
        ILogger<GetSessionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GetSessionResult> Handle(GetSessionQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📋 Retrieving session details for {SessionId}", query.SessionId);

        var session = await _repository.GetByIdAsync(query.SessionId);
        if (session == null)
        {
            _logger.LogWarning("❌ Session {SessionId} not found", query.SessionId);
            return new GetSessionResult
            {
                Success = false,
                ErrorMessage = "Session not found"
            };
        }

        var messages = await _repository.GetSessionMessagesAsync(query.SessionId, cancellationToken);
        var messageCount = messages.Count();

        var sessionData = new SessionData
        {
            SessionId = session.Id,
            UserId = session.UserId,
            IsActive = session.IsActive,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            MessageCount = messageCount
        };

        _logger.LogInformation("✅ Retrieved session {SessionId} with {MessageCount} messages", 
            query.SessionId, messageCount);

        return new GetSessionResult
        {
            Success = true,
            Data = sessionData
        };
    }
}
