using ChatAI.Application.DTOs;
using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to authenticate an admin user
/// </summary>
public class LoginCommand : IRequest<LoginResponseDto>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
