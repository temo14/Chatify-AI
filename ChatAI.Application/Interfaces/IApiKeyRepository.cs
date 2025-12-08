using ChatAI.Domain.Entities;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Repository for API key operations
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default);
}
