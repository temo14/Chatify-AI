using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository for managing Meta channel connections
/// </summary>
public interface IMetaChannelConnectionRepository
{
    /// <summary>
    /// Get connection by ID (tenant-scoped via global query filter)
    /// </summary>
    Task<MetaChannelConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get connection by webhook ID (bypasses tenant filter - used in webhook endpoint)
    /// </summary>
    Task<MetaChannelConnection?> GetByWebhookIdAsync(Guid webhookId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get connection by ID without tenant filter (used in webhook processing before tenant context is set)
    /// </summary>
    Task<MetaChannelConnection?> GetByIdWithoutTenantFilterAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all connections for current tenant
    /// </summary>
    Task<List<MetaChannelConnection>> GetAllForTenantAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get connections by channel for current tenant
    /// </summary>
    Task<List<MetaChannelConnection>> GetByChannelAsync(MetaChannel channel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active connections with token expiry warning
    /// </summary>
    Task<List<MetaChannelConnection>> GetConnectionsWithTokenExpiryWarningAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create new connection
    /// </summary>
    Task<MetaChannelConnection> CreateAsync(MetaChannelConnection connection, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update existing connection
    /// </summary>
    Task UpdateAsync(MetaChannelConnection connection, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete connection
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a channel identity already exists (prevents duplicate connections)
    /// </summary>
    Task<bool> ChannelIdentityExistsAsync(MetaChannel channel, string identityValue, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Find connection by channel-specific identity field (bypasses tenant filter for webhook routing)
    /// </summary>
    Task<MetaChannelConnection?> FindByChannelIdentityAsync(MetaChannel channel, string identityValue, CancellationToken cancellationToken = default);
}
