using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get conversation history (Read operation)
/// </summary>
public record GetConversationHistoryQuery : IRequest<List<ChatMessage>>
{
    public string SessionId { get; init; } = string.Empty;
    public int MaxMessages { get; init; } = 20;
}
