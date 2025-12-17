using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Features.Feedback.GetFeedback;

/// <summary>
/// Query to get feedback by ID (Read operation)
/// </summary>
public record GetFeedbackQuery : IRequest<MessageFeedback?>
{
    public Guid FeedbackId { get; init; }
}
