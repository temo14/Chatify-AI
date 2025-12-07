using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get all distinct configuration categories (Read operation)
/// </summary>
public record GetConfigurationCategoriesQuery : IRequest<IEnumerable<string>>
{
}
