using ChatAI.Domain.Entities;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing tenants (customers)
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(
        ChatDbContext context,
        ILogger<TenantRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLower(), ct);
    }

    public async Task<Tenant?> GetByCustomDomainAsync(string domain, CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.CustomDomain == domain.ToLower(), ct);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Slug == slug.ToLower(), ct);
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenants(CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Settings)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Tenants
            .Include(t => t.Settings)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        string? planTier,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tenants
            .Include(t => t.Settings)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search) ||
                t.Slug.ToLower().Contains(search) ||
                t.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(planTier))
        {
            query = query.Where(t => t.PlanTier == planTier);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Tenant> AddAsync(Tenant entity, CancellationToken ct = default)
    {
        // Ensure slug is lowercase
        entity.Slug = entity.Slug.ToLower();
        if (entity.CustomDomain != null)
        {
            entity.CustomDomain = entity.CustomDomain.ToLower();
        }

        await _context.Tenants.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created tenant {TenantId} ({TenantSlug})", entity.Id, entity.Slug);
        
        return entity;
    }

    public async Task UpdateAsync(Tenant entity, CancellationToken ct = default)
    {
        entity.Slug = entity.Slug.ToLower();
        if (entity.CustomDomain != null)
        {
            entity.CustomDomain = entity.CustomDomain.ToLower();
        }

        _context.Tenants.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Updated tenant {TenantId}", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant != null)
        {
            // Soft delete by marking inactive
            tenant.IsActive = false;
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Soft deleted tenant {TenantId}", id);
        }
    }

    public async Task IncrementMessageCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant != null)
        {
            tenant.CurrentMonthMessages++;
            tenant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task ResetMonthlyMessageCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant != null)
        {
            tenant.CurrentMonthMessages = 0;
            tenant.BillingPeriodStart = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Reset monthly message count for tenant {TenantId}", tenantId);
        }
    }

    public async Task UpdateDocumentCountAsync(Guid tenantId, int count, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant != null)
        {
            tenant.CurrentDocumentCount = count;
            await _context.SaveChangesAsync(ct);
        }
    }
}
