using ChatAI.Api.DTOs;
using ChatAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        var result = await _chatService.HandleAsync(dto.ToDomain());
        return Ok(ChatResponseDto.FromDomain(result));
    }
}
