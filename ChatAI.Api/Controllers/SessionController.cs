using ChatAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Session management and export controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        IChatSessionRepository sessionRepository,
        ILogger<SessionController> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Export chat history for a session as JSON
    /// </summary>
    [HttpGet("{sessionId}/export/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportAsJson(string sessionId)
    {
        _logger.LogInformation("Exporting session {SessionId} as JSON", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found" });
        }

        var messages = await _sessionRepository.GetSessionMessagesAsync(sessionId, CancellationToken.None);

        var messageList = messages.Select(m => new
        {
            role = m.Role.ToString(),
            content = m.Content,
            timestamp = m.Timestamp
        }).ToList();

        var export = new
        {
            sessionId = session.Id,
            userId = session.UserId,
            createdAt = session.CreatedAt,
            lastActivityAt = session.LastActivityAt,
            messageCount = messageList.Count,
            messages = messageList
        };

        return Ok(export);
    }

    /// <summary>
    /// Export chat history for a session as CSV
    /// </summary>
    [HttpGet("{sessionId}/export/csv")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExportAsCsv(string sessionId)
    {
        _logger.LogInformation("Exporting session {SessionId} as CSV", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found" });
        }

        var messages = await _sessionRepository.GetSessionMessagesAsync(sessionId, CancellationToken.None);

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Role,Content");

        foreach (var message in messages)
        {
            var content = message.Content.Replace("\"", "\"\""); // Escape quotes
            csv.AppendLine($"\"{message.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{message.Role}\",\"{content}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"chat-history-{sessionId}.csv");
    }

    /// <summary>
    /// Get session information
    /// </summary>
    [HttpGet("{sessionId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found" });
        }

        var messages = await _sessionRepository.GetSessionMessagesAsync(sessionId, CancellationToken.None);
        var messageList = messages.ToList();
        var count = messageList.Count;

        return Ok(new
        {
            sessionId = session.Id,
            userId = session.UserId,
            isActive = session.IsActive,
            createdAt = session.CreatedAt,
            lastActivityAt = session.LastActivityAt,
            messageCount = count
        });
    }
}
