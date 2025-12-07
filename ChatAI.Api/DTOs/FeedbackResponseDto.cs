using ChatAI.Domain.Enums;

namespace ChatAI.Api.DTOs;

/// <summary>
/// DTO for feedback response
/// </summary>
public class FeedbackResponseDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public FeedbackCategory? Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
