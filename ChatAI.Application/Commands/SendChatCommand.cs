using ChatAI.Application.Models.Response;
using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to send a chat message (Write operation)
/// </summary>
public record SendChatCommand : IRequest<ChatResponse>
{
    public string? UserId { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public bool UseTools { get; init; } = true;
}
