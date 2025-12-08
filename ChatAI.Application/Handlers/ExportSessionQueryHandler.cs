using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for ExportSessionQuery - exports session data in requested format
/// </summary>
public class ExportSessionQueryHandler : IRequestHandler<ExportSessionQuery, ExportSessionResult>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<ExportSessionQueryHandler> _logger;

    public ExportSessionQueryHandler(
        IChatSessionRepository repository,
        ILogger<ExportSessionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ExportSessionResult> Handle(ExportSessionQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📤 Exporting session {SessionId} as {Format}", query.SessionId, query.Format);

        var session = await _repository.GetByIdAsync(query.SessionId);
        if (session == null)
        {
            _logger.LogWarning("❌ Session {SessionId} not found", query.SessionId);
            return new ExportSessionResult
            {
                Success = false,
                ErrorMessage = "Session not found"
            };
        }

        var messages = await _repository.GetSessionMessagesAsync(query.SessionId, cancellationToken);
        var messageList = messages.ToList();

        var exportData = new SessionExportData
        {
            SessionId = session.Id,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            MessageCount = messageList.Count,
            Messages = messageList.Select(m => new ExportedMessage
            {
                Role = m.Role.ToString(),
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList()
        };

        _logger.LogInformation("✅ Successfully exported session {SessionId} with {MessageCount} messages", 
            query.SessionId, messageList.Count);

        return new ExportSessionResult
        {
            Success = true,
            Data = exportData
        };
    }
}
