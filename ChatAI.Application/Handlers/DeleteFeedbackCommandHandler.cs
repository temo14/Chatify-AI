using ChatAI.Application.Commands;
using ChatAI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for DeleteFeedbackCommand - deletes a feedback entry
/// </summary>
public class DeleteFeedbackCommandHandler : IRequestHandler<DeleteFeedbackCommand, bool>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<DeleteFeedbackCommandHandler> _logger;

    public DeleteFeedbackCommandHandler(
        IFeedbackRepository feedbackRepository,
        ILogger<DeleteFeedbackCommandHandler> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(DeleteFeedbackCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🗑️ Deleting feedback {FeedbackId}", command.FeedbackId);

        var feedback = await _feedbackRepository.GetByIdAsync(command.FeedbackId, cancellationToken);
        if (feedback == null)
        {
            _logger.LogWarning("⚠️ Feedback {FeedbackId} not found", command.FeedbackId);
            return false;
        }

        await _feedbackRepository.DeleteAsync(command.FeedbackId, cancellationToken);
        _logger.LogInformation("✅ Feedback {FeedbackId} deleted successfully", command.FeedbackId);
        return true;
    }
}
