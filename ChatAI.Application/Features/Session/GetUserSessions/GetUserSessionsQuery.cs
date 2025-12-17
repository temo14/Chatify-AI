using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Features.Session.GetUserSessions;

/// <summary>
/// Query to get user's active sessions
/// </summary>
public record GetUserSessionsQuery : IRequest<List<ChatSession>>
{
    public string UserId { get; init; } = string.Empty;
    public bool OnlyActive { get; init; } = true;
}
