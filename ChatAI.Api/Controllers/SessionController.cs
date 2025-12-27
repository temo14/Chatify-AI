using ChatAI.Application.Features.Session.ExportSession;
using ChatAI.Application.Features.Session.GetSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Session management and export controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        ISender sender,
        ILogger<SessionController> logger)
    {
        _sender = sender;
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
        var query = new ExportSessionQuery
        {
            SessionId = sessionId,
            Format = ExportFormat.Json
        };

        var result = await _sender.Send(query);

        if (!result.Success)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        var export = new
        {
            sessionId = result.Data!.SessionId,
            userId = result.Data.UserId,
            createdAt = result.Data.CreatedAt,
            lastActivityAt = result.Data.LastActivityAt,
            messageCount = result.Data.MessageCount,
            messages = result.Data.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content,
                timestamp = m.Timestamp
            })
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
        var query = new ExportSessionQuery
        {
            SessionId = sessionId,
            Format = ExportFormat.Csv
        };

        var result = await _sender.Send(query);

        if (!result.Success)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Role,Content");

        foreach (var message in result.Data!.Messages)
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
        var query = new GetSessionQuery { SessionId = sessionId };
        var result = await _sender.Send(query);

        if (!result.Success)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        return Ok(new
        {
            sessionId = result.Data!.SessionId,
            userId = result.Data.UserId,
            isActive = result.Data.IsActive,
            createdAt = result.Data.CreatedAt,
            lastActivityAt = result.Data.LastActivityAt,
            messageCount = result.Data.MessageCount
        });
    }
}
