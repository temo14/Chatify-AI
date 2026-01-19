using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository for deduplicating inbound Meta messages
/// </summary>
public interface IMetaInboundDedupeRepository
{
    /// <summary>
    /// Check if message was already received
    /// </summary>
    Task<bool> IsMessageProcessedAsync(Guid connectionId, string metaMessageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record received message for deduplication
    /// Returns true if inserted (first time), false if already exists
    /// </summary>
    Task<bool> RecordMessageAsync(Guid connectionId, string metaMessageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up old dedupe records (older than retention period)
    /// </summary>
    Task CleanupOldRecordsAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
}
