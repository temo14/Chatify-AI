using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly ChatDbContext _context;
    
    public AdminUserRepository(ChatDbContext context)
    {
        _context = context;
    }
    
    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }
    
    public async Task<AdminUser> CreateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    public async Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        _context.AdminUsers.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .AnyAsync(u => u.Username == username, cancellationToken);
    }
}
