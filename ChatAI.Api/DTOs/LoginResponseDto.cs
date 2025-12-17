namespace ChatAI.Api.DTOs;

/// <summary>
/// Response DTO after successful login
/// </summary>
public class LoginResponseDto
{
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
