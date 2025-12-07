using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get configuration by key (Read operation)
/// </summary>
public record GetConfigurationQuery : IRequest<AdminConfiguration?>
{
    public string Key { get; init; } = string.Empty;
}
