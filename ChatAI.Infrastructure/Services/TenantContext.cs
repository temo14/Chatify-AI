using ChatAI.Domain.Interfaces.Services;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Scoped service that holds the current tenant context for the request
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;
    private string? _tenantSlug;

    public Guid? TenantId => _tenantId;
    
    public Guid RequiredTenantId => _tenantId 
        ?? throw new InvalidOperationException("Tenant context is not set. Ensure TenantResolutionMiddleware ran and a tenant was resolved.");
    
    public string? TenantSlug => _tenantSlug;
    public bool HasTenant => _tenantId.HasValue;

    public void SetTenant(Guid tenantId, string tenantSlug)
    {
        _tenantId = tenantId;
        _tenantSlug = tenantSlug;
    }

    public void Clear()
    {
        _tenantId = null;
        _tenantSlug = null;
    }
}
