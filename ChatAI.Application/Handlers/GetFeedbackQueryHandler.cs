using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for GetFeedbackQuery - retrieves a single feedback entry
/// </summary>
public class GetFeedbackQueryHandler : IRequestHandler<GetFeedbackQuery, MessageFeedback?>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<GetFeedbackQueryHandler> _logger;

    public GetFeedbackQueryHandler(
        IFeedbackRepository feedbackRepository,
        ILogger<GetFeedbackQueryHandler> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MessageFeedback?> Handle(GetFeedbackQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Getting feedback {FeedbackId}", query.FeedbackId);

        var feedback = await _feedbackRepository.GetByIdAsync(query.FeedbackId, cancellationToken);

        if (feedback == null)
        {
            _logger.LogWarning("⚠️ Feedback {FeedbackId} not found", query.FeedbackId);
        }

        return feedback;
    }
}
