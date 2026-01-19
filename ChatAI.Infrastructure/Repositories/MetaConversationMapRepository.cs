using ChatAI.Domain.Entities;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for Meta conversation mappings (external user ID to session ID)
/// </summary>
public class MetaConversationMapRepository : IMetaConversationMapRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<MetaConversationMapRepository> _logger;
    
    public MetaConversationMapRepository(
        ChatDbContext context,
        ILogger<MetaConversationMapRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<MetaConversationMap?> GetByExternalUserIdAsync(Guid connectionId, string externalUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MetaConversationMaps
            .FirstOrDefaultAsync(m => m.ConnectionId == connectionId && m.ExternalUserId == externalUserId, cancellationToken);
    }
    
    public async Task<MetaConversationMap> CreateAsync(MetaConversationMap map, CancellationToken cancellationToken = default)
    {
        map.CreatedAt = DateTime.UtcNow;
        map.LastActivityAt = DateTime.UtcNow;
        
        _context.MetaConversationMaps.Add(map);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created conversation map for connection {ConnectionId}, external user {ExternalUserId} -> session {SessionId}",
            map.ConnectionId, map.ExternalUserId, map.ChatSessionId);
        
        return map;
    }
    
    public async Task UpdateLastActivityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var map = await _context.MetaConversationMaps.FindAsync(new object[] { id }, cancellationToken);
        if (map != null)
        {
            map.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetOptOutAsync(Guid id, bool isOptedOut, CancellationToken cancellationToken = default)
    {
        var map = await _context.MetaConversationMaps.FindAsync(new object[] { id }, cancellationToken);
        if (map == null)
        {
            return;
        }

        map.IsOptedOut = isOptedOut;
        if (isOptedOut)
        {
            map.OptedOutAt = DateTime.UtcNow;
        }
        else
        {
            map.OptedInAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
