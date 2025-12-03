using ChatAI.Domain.Entities;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Repository for managing chat sessions and their messages
/// </summary>
public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken ct = default);
    Task<ChatSession> AddAsync(ChatSession entity, CancellationToken ct = default);
    Task UpdateAsync(ChatSession entity, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Get the most recent active session for a user
    /// </summary>
    Task<ChatSession?> GetActiveSessionByUserIdAsync(string userId, CancellationToken ct = default);
    
    /// <summary>
    /// Get all sessions for a user (most recent first)
    /// </summary>
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId, int limit = 10, CancellationToken ct = default);
    
    /// <summary>
    /// Get all messages for a specific session
    /// </summary>
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Add a message to a session
    /// </summary>
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    
    /// <summary>
    /// Add multiple messages in a single transaction
    /// </summary>
    Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default);
}
