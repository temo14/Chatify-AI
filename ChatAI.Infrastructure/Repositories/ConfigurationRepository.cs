using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Repositories;

/// <summary>
/// Repository for managing admin configuration settings
/// Handles runtime configuration without redeployment
/// </summary>
public class ConfigurationRepository : IConfigurationRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ConfigurationRepository> _logger;

    public ConfigurationRepository(
        ChatDbContext context,
        ILogger<ConfigurationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdminConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<AdminConfiguration?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, ct);
    }

    public async Task<IEnumerable<AdminConfiguration>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AdminConfiguration>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .Where(c => c.Category == category)
            .OrderBy(c => c.Key)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AdminConfiguration>> GetActiveConfigurationsAsync(CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .Where(c => !string.IsNullOrEmpty(c.Category))
            .Select(c => c.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
    {
        return await _context.AdminConfigurations
            .AsNoTracking()
            .AnyAsync(c => c.Key == key, ct);
    }

    public async Task<AdminConfiguration> AddAsync(AdminConfiguration entity, CancellationToken ct = default)
    {
        _context.AdminConfigurations.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Configuration '{Key}' added", entity.Key);
        
        return entity;
    }

    public async Task UpdateAsync(AdminConfiguration entity, CancellationToken ct = default)
    {
        _context.AdminConfigurations.Update(entity);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("✅ Configuration '{Key}' updated", entity.Key);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var config = await _context.AdminConfigurations.FindAsync(new object[] { id }, ct);
        if (config == null)
        {
            _logger.LogWarning("⚠️ Configuration with ID {ConfigId} not found for deletion", id);
            return;
        }

        _context.AdminConfigurations.Remove(config);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("🗑️ Configuration '{Key}' deleted", config.Key);
    }
}
