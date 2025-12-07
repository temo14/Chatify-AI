namespace ChatAI.Api.DTOs;

/// <summary>
/// DTO for feedback statistics response
/// </summary>
public class FeedbackStatsDto
{
    public int TotalFeedback { get; set; }
    public int ThumbsUp { get; set; }
    public int ThumbsDown { get; set; }
    public double SatisfactionRate { get; set; }
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
}
