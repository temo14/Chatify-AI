using MediatR;

namespace ChatAI.Application.Features.Configuration.GetConfigurationCategories;

/// <summary>
/// Query to get all distinct configuration categories (Read operation)
/// </summary>
public record GetConfigurationCategoriesQuery : IRequest<IEnumerable<string>>
{
}
