using System.ComponentModel.DataAnnotations;
using ChatAI.Domain.Enums;

namespace ChatAI.Api.DTOs;

/// <summary>
/// DTO for submitting feedback on a message
/// </summary>
public class SubmitFeedbackDto
{
    [Required]
    public Guid MessageId { get; set; }

    public string? UserId { get; set; }

    public string? SessionId { get; set; }

    [Required]
    [Range(-1, 1)]
    public int Rating { get; set; } // 1 for thumbs up, -1 for thumbs down

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public FeedbackCategory? Category { get; set; }
}
