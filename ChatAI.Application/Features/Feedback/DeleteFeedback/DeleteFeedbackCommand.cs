using MediatR;

namespace ChatAI.Application.Features.Feedback.DeleteFeedback;

/// <summary>
/// Command to delete feedback (Write operation)
/// </summary>
public record DeleteFeedbackCommand : IRequest<bool>
{
    public Guid FeedbackId { get; init; }
}
