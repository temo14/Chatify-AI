using ChatAI.Domain.Entities;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for Meta inbound message deduplication
/// </summary>
public class MetaInboundDedupeRepository : IMetaInboundDedupeRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<MetaInboundDedupeRepository> _logger;
    
    public MetaInboundDedupeRepository(
        ChatDbContext context,
        ILogger<MetaInboundDedupeRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<bool> IsMessageProcessedAsync(Guid connectionId, string metaMessageId, CancellationToken cancellationToken = default)
    {
        return await _context.MetaInboundDedupes
            .AnyAsync(d => d.ConnectionId == connectionId && d.MetaMessageId == metaMessageId, cancellationToken);
    }
    
    public async Task<bool> RecordMessageAsync(Guid connectionId, string metaMessageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dedupe = new MetaInboundDedupe
            {
                ConnectionId = connectionId,
                MetaMessageId = metaMessageId,
                ReceivedAt = DateTime.UtcNow
            };
            
            _context.MetaInboundDedupes.Add(dedupe);
            await _context.SaveChangesAsync(cancellationToken);
            
            return true; // Successfully inserted (first time)
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true ||
                                          ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
        {
            // Duplicate message (already processed)
            _logger.LogDebug("Duplicate message detected: {ConnectionId}/{MessageId}", connectionId, metaMessageId);
            return false;
        }
    }
    
    public async Task CleanupOldRecordsAsync(int retentionDays = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        var deleted = await _context.MetaInboundDedupes
            .Where(d => d.ReceivedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
        
        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old dedupe records older than {Days} days", deleted, retentionDays);
        }
    }
}
