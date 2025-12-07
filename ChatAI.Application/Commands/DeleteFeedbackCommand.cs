using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to delete feedback (Write operation)
/// </summary>
public record DeleteFeedbackCommand : IRequest<bool>
{
    public Guid FeedbackId { get; init; }
}
