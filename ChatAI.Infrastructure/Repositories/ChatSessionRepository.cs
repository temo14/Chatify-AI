using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing chat sessions and messages with database persistence
/// Multi-tenant aware - automatically filters by current tenant
/// </summary>
public class ChatSessionRepository : IChatSessionRepository
{
    private readonly ChatDbContext _context;
    private readonly ITenantContext _tenantContext; // Multi-tenancy support
    private readonly ILogger<ChatSessionRepository> _logger;

    public ChatSessionRepository(
        ChatDbContext context, 
        ITenantContext tenantContext,
        ILogger<ChatSessionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatSession?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var session = await _context.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
            
        // Explicit tenant validation for additional security
        if (session != null && session.TenantId != _tenantContext.TenantId)
        {
            _logger.LogWarning("⚠️ Attempted cross-tenant session access: {SessionId}", id);
            return null;
        }
        
        return session;
    }

    public async Task<IEnumerable<ChatSession>> GetAllAsync(CancellationToken ct = default)
    {
        // Ensure tenant context is set
        if (_tenantContext.TenantId == null)
        {
            _logger.LogWarning("⚠️ GetAllAsync called without tenant context");
            return Enumerable.Empty<ChatSession>();
        }
        
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
        entity.TenantId = _tenantContext.RequiredTenantId; // Set tenant from context
        
        _context.ChatSessions.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Created session {SessionId} for user {UserId} in tenant {TenantId}", 
            entity.Id, entity.UserId, entity.TenantId);
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
            _logger.LogInformation("✅ Deleted session {SessionId}", id);
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
            .ToListAsync(ct).ConfigureAwait(false);
        
        _logger.LogDebug("Loaded {Count} messages for session {SessionId}", messages.Count, sessionId);
        return messages;
    }

    public async Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(string sessionId, int skip, int take, CancellationToken ct = default)
    {
        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);
        
        _logger.LogDebug("Loaded {Count} messages (skip: {Skip}, take: {Take}) for session {SessionId}", 
            messages.Count, skip, take, sessionId);
        return messages;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.Id == Guid.Empty)
            message.Id = Guid.NewGuid();
        
        if (message.Timestamp == default)
            message.Timestamp = DateTime.UtcNow;
        
        message.TenantId = _tenantContext.RequiredTenantId; // Set tenant from context
        
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("Added message {MessageId} to session {SessionId} in tenant {TenantId}", 
            message.Id, message.SessionId, message.TenantId);
        return message;
    }

    public async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var tenantId = _tenantContext.RequiredTenantId; // Get tenant once
        
        foreach (var message in messageList)
        {
            if (message.Id == Guid.Empty)
                message.Id = Guid.NewGuid();
            
            if (message.Timestamp == default)
                message.Timestamp = DateTime.UtcNow;
            
            message.TenantId = tenantId; // Set tenant from context
        }
        
        _context.ChatMessages.AddRange(messageList);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Added {Count} messages in batch", messageList.Count);
    }
}
