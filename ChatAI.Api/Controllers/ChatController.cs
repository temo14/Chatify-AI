using ChatAI.Api.DTOs;
using ChatAI.Application.Features.Chat.GetConversationHistory;
using ChatAI.Application.Features.Chat.SendMessage;
using ChatAI.Application.Features.Session.GetUserSessions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Chat controller for non-streaming chat interactions
/// Thin controller - delegates all logic to Application layer via CQRS (MediatR)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISender sender, ILogger<ChatController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a chat message and get AI response
    /// SessionId Flow: null/empty = new chat, existing = continue conversation
    /// </summary>
    /// <param name="dto">Chat request with message and optional sessionId</param>
    /// <response code="200">Returns AI response with sessionId</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Chat request - UserId: {UserId}, SessionId: {SessionId}", 
            dto.UserId ?? "anonymous", dto.SessionId ?? "new");
    
        var command = new SendChatCommand
        {
            UserId = dto.UserId,
            Message = dto.Message,
            SessionId = dto.SessionId,
            UseTools = dto.UseTools
        };
        
        var result = await _sender.Send(command);
        return Ok(ChatResponseDto.FromDomain(result));
    }

    /// <summary>
    /// Get all chat sessions for a user
    /// </summary>
    /// <param name="userId">User identifier (required)</param>
    /// <param name="onlyActive">Only return active sessions (default: true)</param>
    /// <response code="200">Returns list of user sessions</response>
    /// <response code="400">UserId parameter missing</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSessions([FromQuery] string? userId, [FromQuery] bool onlyActive = true)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "UserId parameter is required" });
        }

        var query = new GetUserSessionsQuery
        {
            UserId = userId,
            OnlyActive = onlyActive
        };
        
        var sessions = await _sender.Send(query);
        return Ok(sessions);
    }

    /// <summary>
    /// Get conversation history for a specific session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="maxMessages">Maximum messages to return (default: 20)</param>
    /// <response code="200">Returns conversation messages</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("sessions/{sessionId}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversationHistory(
        string sessionId, 
        [FromQuery] int maxMessages = 20)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { error = "SessionId parameter is required" });
        }

        if (maxMessages < 1 || maxMessages > 100)
        {
            return BadRequest(new { error = "MaxMessages must be between 1 and 100" });
        }

        var query = new GetConversationHistoryQuery
        {
            SessionId = sessionId,
            MaxMessages = maxMessages
        };
        
        var messages = await _sender.Send(query);
        return Ok(messages);
    }
}

