using ChatAI.Api.DTOs;
using ChatAI.Application.Commands;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISender sender, ILogger<ChatController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message and get AI response.
    /// SessionId Flow:
    /// - New chat: Send sessionId as null/empty → Response includes new sessionId → Client stores it
    /// - Continue chat: Send stored sessionId → Response uses existing conversation history
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        _logger.LogInformation("Chat request - UserId: {UserId}, SessionId: {SessionId}, NewChat: {IsNewChat}", 
            dto.UserId ?? "null", dto.SessionId ?? "null (new chat)", string.IsNullOrWhiteSpace(dto.SessionId));
        
        // Use CQRS command pattern
        var command = new SendChatCommand
        {
            UserId = dto.UserId,
            Message = dto.Message,
            SessionId = dto.SessionId, // Null = create new session, Non-null = continue existing
            UseTools = dto.UseTools
        };
        
        var result = await _sender.Send(command);
        
        // Response ALWAYS includes sessionId (new or existing)
        // Client should store this sessionId for continuing the conversation
        return Ok(ChatResponseDto.FromDomain(result));
    }

    /// <summary>
    /// Get all chat sessions for a user (requires userId parameter)
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] string? userId, [FromQuery] bool onlyActive = true)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "UserId parameter is required" });
        }

        // Use CQRS query pattern
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
    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetConversationHistory(
        string sessionId, 
        [FromQuery] int maxMessages = 20)
    {
        // Use CQRS query pattern
        var query = new GetConversationHistoryQuery
        {
            SessionId = sessionId,
            MaxMessages = maxMessages
        };
        
        var messages = await _sender.Send(query);
        return Ok(messages);
    }
}

