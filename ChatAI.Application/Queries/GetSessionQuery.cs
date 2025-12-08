using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get detailed session information
/// </summary>
public record GetSessionQuery : IRequest<GetSessionResult>
{
    public string SessionId { get; init; } = string.Empty;
}

/// <summary>
/// Result of get session query
/// </summary>
public class GetSessionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public SessionData? Data { get; set; }
}

/// <summary>
/// Session data
/// </summary>
public class SessionData
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}
