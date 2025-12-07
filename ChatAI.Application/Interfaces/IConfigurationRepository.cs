using ChatAI.Domain.Entities;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Repository interface for AdminConfiguration operations
/// </summary>
public interface IConfigurationRepository : IRepository<AdminConfiguration>
{
    Task<AdminConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminConfiguration>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminConfiguration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);
}
