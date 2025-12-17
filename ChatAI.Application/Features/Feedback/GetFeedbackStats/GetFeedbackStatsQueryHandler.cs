using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;

using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Feedback.GetFeedbackStats;

/// <summary>
/// Handler for GetFeedbackStatsQuery - retrieves feedback statistics and analytics
/// </summary>
public class GetFeedbackStatsQueryHandler : IRequestHandler<GetFeedbackStatsQuery, FeedbackStats>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<GetFeedbackStatsQueryHandler> _logger;

    public GetFeedbackStatsQueryHandler(
        IFeedbackRepository feedbackRepository,
        ILogger<GetFeedbackStatsQueryHandler> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeedbackStats> Handle(GetFeedbackStatsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📊 Getting feedback statistics");

        var (total, thumbsUp, thumbsDown, categoryBreakdown) = await _feedbackRepository.GetStatsAsync(cancellationToken);

        var satisfactionRate = total > 0 ? (double)thumbsUp / total * 100 : 0;

        var stats = new FeedbackStats
        {
            TotalFeedback = total,
            ThumbsUp = thumbsUp,
            ThumbsDown = thumbsDown,
            SatisfactionRate = Math.Round(satisfactionRate, 2),
            CategoryBreakdown = categoryBreakdown
        };

        _logger.LogInformation("✅ Stats: {Total} total, {Up} up, {Down} down, {Rate}% satisfaction", 
            total, thumbsUp, thumbsDown, stats.SatisfactionRate);

        return stats;
    }
}
