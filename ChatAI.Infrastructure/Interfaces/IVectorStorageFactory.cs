using ChatAI.Domain.Interfaces.Services;

namespace ChatAI.Infrastructure.Interfaces;

/// <summary>
/// Factory for creating vector storage instances based on tenant settings
/// </summary>
public interface IVectorStorageFactory
{
    /// <summary>
    /// Create a vector storage instance for the current tenant
    /// </summary>
    Task<IVectorStorage> CreateForCurrentTenantAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a vector storage instance for a specific tenant
    /// </summary>
    Task<IVectorStorage> CreateForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
