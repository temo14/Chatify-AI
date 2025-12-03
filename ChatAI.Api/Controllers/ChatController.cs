using ChatAI.Api.DTOs;
using ChatAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        // Get authenticated user ID from claims (set by ApiKeyAuthMiddleware)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? dto.UserId;
        
        _logger.LogInformation("Chat request from user: {UserId}", userId);
        
        // Override userId with authenticated user
        var request = dto.ToDomain();
        request.UserId = userId;
        
        var result = await _chatService.HandleAsync(request);
        return Ok(ChatResponseDto.FromDomain(result));
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        // TODO: Implement session retrieval
        return Ok(new { userId, message = "Session retrieval coming soon" });
    }
}

