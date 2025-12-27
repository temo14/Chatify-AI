namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Provides access to the current tenant context
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID (from JWT, subdomain, or header)
    /// Nullable - may be null for unauthenticated requests
    /// </summary>
    Guid? TenantId { get; }
    
    /// <summary>
    /// Gets the current tenant ID (non-nullable)
    /// Throws InvalidOperationException if no tenant is set
    /// </summary>
    Guid RequiredTenantId { get; }
    
    /// <summary>
    /// Gets the current tenant slug
    /// </summary>
    string? TenantSlug { get; }
    
    /// <summary>
    /// Whether a tenant is currently resolved
    /// </summary>
    bool HasTenant { get; }
    
    /// <summary>
    /// Set the tenant context (called by middleware)
    /// </summary>
    void SetTenant(Guid tenantId, string tenantSlug);
    
    /// <summary>
    /// Clear the tenant context
    /// </summary>
    void Clear();
}
