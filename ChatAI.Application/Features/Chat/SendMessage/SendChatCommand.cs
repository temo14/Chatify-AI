using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Chat.SendMessage;

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
