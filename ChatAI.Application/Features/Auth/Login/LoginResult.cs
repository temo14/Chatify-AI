namespace ChatAI.Application.Features.Auth.Login;

/// <summary>
/// Result returned after successful login
/// </summary>
public class LoginResult
{
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
