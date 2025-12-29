using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ChatDbContext _context;
    private readonly ITenantContext _tenantContext;
    
    public ApiKeyRepository(ChatDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }
    
    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Query filter automatically applies tenant isolation
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }
    
    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        // Query filter automatically applies tenant isolation
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
    }
    
    public async Task<List<ApiKey>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // Query filter automatically applies tenant isolation
        var query = _context.ApiKeys.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(k => k.IsActive);
        }
        
        return await query.OrderByDescending(k => k.CreatedAt).ToListAsync(cancellationToken);
    }
    
    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        // Set tenant from context (convert Guid to string)
        apiKey.TenantId = _tenantContext.RequiredTenantId.ToString();
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);
        return apiKey;
    }
    
    public async Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
