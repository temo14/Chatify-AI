using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing admin users
/// Multi-tenant aware - automatically filters by current tenant
/// </summary>
public class AdminUserRepository : IAdminUserRepository
{
    private readonly ChatDbContext _context;
    private readonly ITenantContext _tenantContext; // Multi-tenancy support
    
    public AdminUserRepository(ChatDbContext context, ITenantContext tenantContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }
    
    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // Ignore tenant filter for login - admin can belong to any tenant
        return await _context.AdminUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }
    
    public async Task<AdminUser?> GetByUsernameAndTenantAsync(string username, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Get admin user by username AND tenant ID (prevents cross-tenant collision)
        return await _context.AdminUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == username && u.TenantId == tenantId, cancellationToken);
    }

    public async Task<AdminUser?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Get the admin user for a specific tenant
        // Exclude platform admins - they don't belong to a specific tenant
        return await _context.AdminUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && !u.IsPlatformAdmin, cancellationToken);
    }
    
    public async Task<AdminUser> CreateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    public async Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .AnyAsync(u => u.Username == username, cancellationToken);
    }
}
