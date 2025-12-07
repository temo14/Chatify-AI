using ChatAI.Application.Commands;
using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for SubmitFeedbackCommand - creates or updates feedback for a message
/// </summary>
public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Guid>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<SubmitFeedbackCommandHandler> _logger;

    public SubmitFeedbackCommandHandler(
        IFeedbackRepository feedbackRepository,
        ILogger<SubmitFeedbackCommandHandler> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(SubmitFeedbackCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📨 Submitting feedback for message {MessageId}", command.MessageId);

        // Check if feedback already exists for this user and message
        var existing = await _feedbackRepository.GetByMessageIdAndUserIdAsync(
            command.MessageId, 
            command.UserId, 
            cancellationToken);

        if (existing != null)
        {
            // Update existing feedback
            existing.Rating = command.Rating;
            existing.Comment = command.Comment;
            existing.Category = command.Category;

            await _feedbackRepository.UpdateAsync(existing, cancellationToken);

            _logger.LogInformation("✅ Updated existing feedback {FeedbackId}", existing.Id);
            return existing.Id;
        }

        // Create new feedback
        var feedback = new MessageFeedback
        {
            Id = Guid.NewGuid(),
            MessageId = command.MessageId,
            UserId = command.UserId ?? string.Empty,
            SessionId = command.SessionId ?? string.Empty,
            Rating = command.Rating,
            Comment = command.Comment,
            Category = command.Category,
            IpAddress = command.IpAddress,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _feedbackRepository.AddAsync(feedback, cancellationToken);

        _logger.LogInformation("✅ Created new feedback {FeedbackId}", result.Id);
        return result.Id;
    }
}
