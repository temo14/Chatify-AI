namespace ChatAI.Api.DTOs;

/// <summary>
/// Request DTO for admin login
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Tenant slug to identify which organization the admin belongs to
    /// Required to prevent username collisions between tenants
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
