using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for MessageFeedback operations
/// </summary>
public interface IFeedbackRepository : IRepository<MessageFeedback>
{
    Task<MessageFeedback?> GetByMessageIdAndUserIdAsync(Guid messageId, string? userId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<MessageFeedback> Items, int TotalCount)> GetPagedAsync(int? rating, string? sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(int Total, int ThumbsUp, int ThumbsDown, Dictionary<string, int> CategoryBreakdown)> GetStatsAsync(CancellationToken cancellationToken = default);
}
