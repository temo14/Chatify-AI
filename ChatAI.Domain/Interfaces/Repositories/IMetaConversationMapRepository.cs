using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Repositories;

/// <summary>
/// Repository for mapping external Meta user IDs to internal session IDs
/// </summary>
public interface IMetaConversationMapRepository
{
    /// <summary>
    /// Get mapping by connection and external user ID
    /// </summary>
    Task<MetaConversationMap?> GetByExternalUserIdAsync(Guid connectionId, string externalUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create new conversation mapping
    /// </summary>
    Task<MetaConversationMap> CreateAsync(MetaConversationMap map, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update last activity timestamp
    /// </summary>
    Task UpdateLastActivityAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set opt-out state (STOP/START) for a conversation
    /// </summary>
    Task SetOptOutAsync(Guid id, bool isOptedOut, CancellationToken cancellationToken = default);
}
