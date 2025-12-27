using System.Security.Claims;
using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Authentication service for admin users
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
    
    /// <summary>
    /// Generate a JWT token for an admin user
    /// </summary>
    string GenerateJwtToken(AdminUser user, bool rememberMe = false);
    
    /// <summary>
    /// Validate and parse a JWT token
    /// </summary>
    Guid? ValidateJwtToken(string token);
    
    /// <summary>
    /// Create claims array for an admin user
    /// </summary>
    Claim[] CreateUserClaims(AdminUser user);
}
