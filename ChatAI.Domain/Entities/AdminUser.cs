namespace ChatAI.Domain.Entities;

/// <summary>
/// Admin user entity for authentication and authorization
/// </summary>
public class AdminUser
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tenant (customer) that owns this admin user
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Unique username for login
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// BCrypt hashed password
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Personal email address for this specific admin user
    /// Used for: Password reset, security alerts, admin notifications
    /// Each admin can have their own email address
    /// Example: john@musicstudio.com
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Full name for display
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Platform admin flag - only Dott staff can create/manage customer tenants
    /// TRUE = Dott staff (platform admin) - can manage all tenants
    /// FALSE = Customer admin (tenant admin) - can only manage their own tenant's data
    /// </summary>
    public bool IsPlatformAdmin { get; set; } = false;
    
    /// <summary>
    /// Number of failed login attempts (for lockout)
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;
    
    /// <summary>
    /// When the account is locked until (null if not locked)
    /// </summary>
    public DateTime? LockedUntil { get; set; }
}
