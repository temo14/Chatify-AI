namespace ChatAI.Application.Features.Auth.Login;

/// <summary>
/// Result returned after successful login
/// </summary>
public class LoginResult
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid TenantId { get; set; } // Multi-tenancy support
    public string Role { get; set; } = string.Empty; // PlatformAdmin or TenantAdmin
}
