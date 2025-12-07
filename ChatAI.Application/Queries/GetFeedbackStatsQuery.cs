using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get feedback statistics (Read operation)
/// </summary>
public record GetFeedbackStatsQuery : IRequest<FeedbackStats>
{
}

public class FeedbackStats
{
    public int TotalFeedback { get; set; }
    public int ThumbsUp { get; set; }
    public int ThumbsDown { get; set; }
    public double SatisfactionRate { get; set; }
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
}
