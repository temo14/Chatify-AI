using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Tenant operations
/// </summary>
public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByCustomDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetActiveTenants(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        string? planTier,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task IncrementMessageCountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task ResetMonthlyMessageCountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpdateDocumentCountAsync(Guid tenantId, int count, CancellationToken cancellationToken = default);
}
