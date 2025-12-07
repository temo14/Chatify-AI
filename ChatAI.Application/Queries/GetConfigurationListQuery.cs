using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get all configurations with optional filters (Read operation)
/// </summary>
public record GetConfigurationListQuery : IRequest<IEnumerable<AdminConfiguration>>
{
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
}
