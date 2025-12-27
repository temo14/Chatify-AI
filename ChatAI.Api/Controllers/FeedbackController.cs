using ChatAI.Api.DTOs;
using ChatAI.Application.Features.Feedback.DeleteFeedback;
using ChatAI.Application.Features.Feedback.GetFeedback;
using ChatAI.Application.Features.Feedback.GetFeedbackList;
using ChatAI.Application.Features.Feedback.GetFeedbackStats;
using ChatAI.Application.Features.Feedback.SubmitFeedback;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Controller for managing message feedback
/// Thin controller - delegates all logic to Application layer via CQRS (MediatR)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(ISender sender, ILogger<FeedbackController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit or update feedback for a message
    /// </summary>
    /// <param name="messageId">ID of the message to provide feedback for</param>
    /// <param name="dto">Feedback details</param>
    /// <response code="200">Feedback submitted successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeedback(Guid messageId, [FromBody] SubmitFeedbackDto dto)
    {
        var command = new SubmitFeedbackCommand
        {
            MessageId = messageId,
            UserId = dto.UserId,
            SessionId = dto.SessionId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            Category = dto.Category,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var feedbackId = await _sender.Send(command);

        _logger.LogInformation("Feedback submitted for message {MessageId}", messageId);

        return Ok(new { id = feedbackId });
    }

    /// <summary>
    /// Get feedback by ID
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <response code="200">Returns the feedback</response>
    /// <response code="404">Feedback not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedback(Guid id)
    {
        var query = new GetFeedbackQuery { FeedbackId = id };
        var feedback = await _sender.Send(query);

        if (feedback == null)
        {
            return NotFound();
        }

        var dto = new FeedbackResponseDto
        {
            Id = feedback.Id,
            MessageId = feedback.MessageId,
            UserId = feedback.UserId,
            SessionId = feedback.SessionId,
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            Category = feedback.Category,
            CreatedAt = feedback.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get all feedback with optional filters and pagination
    /// </summary>
    /// <param name="rating">Filter by rating (1 or -1)</param>
    /// <param name="sessionId">Filter by session ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <response code="200">Returns paginated feedback list</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFeedback(
        [FromQuery] int? rating, 
        [FromQuery] string? sessionId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var query = new GetFeedbackListQuery
        {
            Rating = rating,
            SessionId = sessionId,
            PageNumber = page,
            PageSize = pageSize
        };

        var (items, totalCount) = await _sender.Send(query);

        var dtos = items.Select(f => new FeedbackResponseDto
        {
            Id = f.Id,
            MessageId = f.MessageId,
            UserId = f.UserId,
            SessionId = f.SessionId,
            Rating = f.Rating,
            Comment = f.Comment,
            Category = f.Category,
            CreatedAt = f.CreatedAt
        });

        return Ok(new { items = dtos, total = totalCount, page, pageSize });
    }

    /// <summary>
    /// Get feedback statistics and analytics
    /// </summary>
    /// <response code="200">Returns feedback statistics</response>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var query = new GetFeedbackStatsQuery();
        var stats = await _sender.Send(query);

        var dto = new FeedbackStatsDto
        {
            TotalFeedback = stats.TotalFeedback,
            ThumbsUp = stats.ThumbsUp,
            ThumbsDown = stats.ThumbsDown,
            SatisfactionRate = stats.SatisfactionRate,
            CategoryBreakdown = stats.CategoryBreakdown
        };

        return Ok(dto);
    }

    /// <summary>
    /// Delete feedback
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <response code="204">Feedback deleted successfully</response>
    /// <response code="404">Feedback not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeedback(Guid id)
    {
        var command = new DeleteFeedbackCommand { FeedbackId = id };
        var result = await _sender.Send(command);

        if (!result)
        {
            return NotFound();
        }

        _logger.LogInformation("Feedback {FeedbackId} deleted", id);

        return NoContent();
    }
}
