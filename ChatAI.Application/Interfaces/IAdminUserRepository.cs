using ChatAI.Domain.Entities;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Repository for admin user operations
/// </summary>
public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AdminUser> CreateAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
}
