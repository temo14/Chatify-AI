using ChatAI.Domain.Enums;
using MediatR;

namespace ChatAI.Application.Features.Feedback.SubmitFeedback;

/// <summary>
/// Command to submit feedback on a chat message (Write operation)
/// </summary>
public record SubmitFeedbackCommand : IRequest<Guid>
{
    public Guid MessageId { get; init; }
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public int Rating { get; init; } // 1 for thumbs up, -1 for thumbs down
    public string? Comment { get; init; }
    public FeedbackCategory? Category { get; init; }
    public string? IpAddress { get; init; }
}
