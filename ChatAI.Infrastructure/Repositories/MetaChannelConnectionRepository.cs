using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for Meta channel connections with multi-tenant support
/// </summary>
public class MetaChannelConnectionRepository : IMetaChannelConnectionRepository
{
    private readonly ChatDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MetaChannelConnectionRepository> _logger;
    
    public MetaChannelConnectionRepository(
        ChatDbContext context,
        ITenantContext tenantContext,
        ILogger<MetaChannelConnectionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<MetaChannelConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Global query filter applies tenant isolation automatically
        return await _context.MetaChannelConnections
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<MetaChannelConnection?> GetByWebhookIdAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        // Bypass tenant filter - webhook ID is globally unique
        return await _context.MetaChannelConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.WebhookId == webhookId, cancellationToken);
    }
    
    public async Task<MetaChannelConnection?> GetByIdWithoutTenantFilterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Bypass tenant filter - used in webhook processing before tenant context is set
        return await _context.MetaChannelConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<List<MetaChannelConnection>> GetAllForTenantAsync(CancellationToken cancellationToken = default)
    {
        // Global query filter applies tenant isolation
        return await _context.MetaChannelConnections
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<MetaChannelConnection>> GetByChannelAsync(MetaChannel channel, CancellationToken cancellationToken = default)
    {
        return await _context.MetaChannelConnections
            .Where(c => c.Channel == channel)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<MetaChannelConnection>> GetConnectionsWithTokenExpiryWarningAsync(CancellationToken cancellationToken = default)
    {
        // Get active connections with token expiry warning (bypasses tenant filter for background job)
        return await _context.MetaChannelConnections
            .IgnoreQueryFilters()
            .Where(c => c.IsActive && c.TokenExpiryWarning)
            .OrderBy(c => c.TokenExpiresAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<MetaChannelConnection> CreateAsync(MetaChannelConnection connection, CancellationToken cancellationToken = default)
    {
        // Ensure tenant ID is set
        if (connection.TenantId == Guid.Empty)
        {
            connection.TenantId = _tenantContext.RequiredTenantId;
        }
        
        connection.CreatedAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;
        
        _context.MetaChannelConnections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created Meta connection {ConnectionId} for tenant {TenantId}, channel {Channel}",
            connection.Id, connection.TenantId, connection.Channel);
        
        return connection;
    }
    
    public async Task UpdateAsync(MetaChannelConnection connection, CancellationToken cancellationToken = default)
    {
        connection.UpdatedAt = DateTime.UtcNow;
        
        _context.MetaChannelConnections.Update(connection);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated Meta connection {ConnectionId}", connection.Id);
    }
    
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await GetByIdAsync(id, cancellationToken);
        if (connection != null)
        {
            _context.MetaChannelConnections.Remove(connection);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Deleted Meta connection {ConnectionId}", id);
        }
    }
    
    public async Task<bool> ChannelIdentityExistsAsync(MetaChannel channel, string identityValue, CancellationToken cancellationToken = default)
    {
        // Check globally (not just current tenant) to prevent duplicate connections across tenants
        return channel switch
        {
            MetaChannel.Messenger => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .AnyAsync(c => c.FacebookPageId == identityValue, cancellationToken),
            
            MetaChannel.Instagram => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .AnyAsync(c => c.InstagramBusinessAccountId == identityValue, cancellationToken),
            
            MetaChannel.WhatsApp => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .AnyAsync(c => c.WhatsAppPhoneNumberId == identityValue, cancellationToken),
            
            _ => false
        };
    }
    
    public async Task<MetaChannelConnection?> FindByChannelIdentityAsync(MetaChannel channel, string identityValue, CancellationToken cancellationToken = default)
    {
        // Bypass tenant filter - used for webhook routing where we don't have tenant context
        return channel switch
        {
            MetaChannel.Messenger => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.FacebookPageId == identityValue && c.IsActive, cancellationToken),
            
            MetaChannel.Instagram => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.InstagramBusinessAccountId == identityValue && c.IsActive, cancellationToken),
            
            MetaChannel.WhatsApp => await _context.MetaChannelConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.WhatsAppPhoneNumberId == identityValue && c.IsActive, cancellationToken),
            
            _ => null
        };
    }
}
