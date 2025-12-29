using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing message feedback
/// Handles CRUD operations and statistics for user feedback
/// Multi-tenant aware - automatically filters by current tenant
/// </summary>
public class FeedbackRepository : IFeedbackRepository
{
    private readonly ChatDbContext _context;
    private readonly ITenantContext _tenantContext; // Multi-tenancy support
    private readonly ILogger<FeedbackRepository> _logger;

    public FeedbackRepository(
        ChatDbContext context,
        ITenantContext tenantContext,
        ILogger<FeedbackRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MessageFeedback?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await _context.MessageFeedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);
            
        // Explicit tenant validation for additional security
        if (feedback != null && feedback.TenantId != _tenantContext.TenantId)
        {
            _logger.LogWarning("⚠️ Attempted cross-tenant feedback access: {FeedbackId}", id);
            return null;
        }
        
        return feedback;
    }

    public async Task<MessageFeedback?> GetByMessageIdAndUserIdAsync(Guid messageId, string? userId, CancellationToken ct = default)
    {
        return await _context.MessageFeedbacks
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId, ct);
    }

    public async Task<IEnumerable<MessageFeedback>> GetAllAsync(CancellationToken ct = default)
    {
        // Ensure tenant context is set
        if (_tenantContext.TenantId == null)
        {
            _logger.LogWarning("⚠️ GetAllAsync called without tenant context");
            return Enumerable.Empty<MessageFeedback>();
        }
        
        return await _context.MessageFeedbacks
            .AsNoTracking()
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<MessageFeedback> Items, int TotalCount)> GetPagedAsync(
        int? rating, 
        string? sessionId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken ct = default)
    {
        var query = _context.MessageFeedbacks.AsNoTracking();

        // Apply filters
        if (rating.HasValue)
        {
            query = query.Where(f => f.Rating == rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query = query.Where(f => f.SessionId == sessionId);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(int Total, int ThumbsUp, int ThumbsDown, Dictionary<string, int> CategoryBreakdown)> GetStatsAsync(
        CancellationToken ct = default)
    {
        var allFeedback = await _context.MessageFeedbacks.AsNoTracking().ToListAsync(ct);

        var total = allFeedback.Count;
        var thumbsUp = allFeedback.Count(f => f.Rating == 1);
        var thumbsDown = allFeedback.Count(f => f.Rating == -1);

        var categoryBreakdown = allFeedback
            .Where(f => f.Category.HasValue)
            .GroupBy(f => f.Category!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return (total, thumbsUp, thumbsDown, categoryBreakdown);
    }

    public async Task<MessageFeedback> AddAsync(MessageFeedback entity, CancellationToken ct = default)
    {
        entity.TenantId = _tenantContext.RequiredTenantId; // Set tenant from context
        
        _context.MessageFeedbacks.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Feedback {FeedbackId} added for message {MessageId} in tenant {TenantId}", 
            entity.Id, entity.MessageId, entity.TenantId);
        
        return entity;
    }

    public async Task UpdateAsync(MessageFeedback entity, CancellationToken ct = default)
    {
        _context.MessageFeedbacks.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Feedback {FeedbackId} updated", entity.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var feedback = await _context.MessageFeedbacks.FindAsync(new object[] { id }, ct);
        if (feedback == null)
        {
            _logger.LogWarning("⚠️ Feedback {FeedbackId} not found for deletion", id);
            return;
        }

        _context.MessageFeedbacks.Remove(feedback);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("🗑️ Feedback {FeedbackId} deleted", id);
    }
}
