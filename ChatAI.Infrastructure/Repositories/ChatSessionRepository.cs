using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing chat sessions and messages with database persistence
/// </summary>
public class ChatSessionRepository : IChatSessionRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ChatSessionRepository> _logger;

    public ChatSessionRepository(ChatDbContext context, ILogger<ChatSessionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatSession?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatSession> AddAsync(ChatSession entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = Guid.NewGuid().ToString();
            
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.LastActivityAt = DateTime.UtcNow;
        
        _context.ChatSessions.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created session {SessionId} for user {UserId}", entity.Id, entity.UserId);
        return entity;
    }

    public async Task UpdateAsync(ChatSession entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.ChatSessions.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("Updated session {SessionId}", entity.Id);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var session = await _context.ChatSessions.FindAsync(new object[] { id }, ct);
        if (session != null)
        {
            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted session {SessionId}", id);
        }
    }

    public async Task<ChatSession?> GetActiveSessionByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId, int limit = 10, CancellationToken ct = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(string sessionId, CancellationToken ct = default)
    {
        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);
        
        _logger.LogDebug("Loaded {Count} messages for session {SessionId}", messages.Count, sessionId);
        return messages;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.Id == Guid.Empty)
            message.Id = Guid.NewGuid();
        
        if (message.Timestamp == default)
            message.Timestamp = DateTime.UtcNow;
        
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("Added message {MessageId} to session {SessionId}", message.Id, message.SessionId);
        return message;
    }

    public async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        foreach (var message in messageList)
        {
            if (message.Id == Guid.Empty)
                message.Id = Guid.NewGuid();
            
            if (message.Timestamp == default)
                message.Timestamp = DateTime.UtcNow;
        }
        
        _context.ChatMessages.AddRange(messageList);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Added {Count} messages in batch", messageList.Count);
    }
}
