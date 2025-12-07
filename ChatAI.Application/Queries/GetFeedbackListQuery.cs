using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get paginated feedback list with filters (Read operation)
/// </summary>
public record GetFeedbackListQuery : IRequest<(IEnumerable<MessageFeedback> Items, int TotalCount)>
{
    public int? Rating { get; init; }
    public string? SessionId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
