using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for GetFeedbackListQuery - retrieves paginated feedback list with filters
/// </summary>
public class GetFeedbackListQueryHandler : IRequestHandler<GetFeedbackListQuery, (IEnumerable<MessageFeedback> Items, int TotalCount)>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<GetFeedbackListQueryHandler> _logger;

    public GetFeedbackListQueryHandler(
        IFeedbackRepository feedbackRepository,
        ILogger<GetFeedbackListQueryHandler> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(IEnumerable<MessageFeedback> Items, int TotalCount)> Handle(
        GetFeedbackListQuery query, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("📖 Getting feedback list - Page {Page}, Size {Size}", query.PageNumber, query.PageSize);

        var result = await _feedbackRepository.GetPagedAsync(
            query.Rating,
            query.SessionId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        _logger.LogInformation("✅ Retrieved {Count} feedback items (Total: {Total})", 
            result.Items.Count(), result.TotalCount);

        return result;
    }
}
