namespace ChatAI.Api.DTOs;

/// <summary>
/// Request DTO for admin login
/// </summary>
public class LoginDto
{
    
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
