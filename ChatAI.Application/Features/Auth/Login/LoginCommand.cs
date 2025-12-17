using MediatR;

namespace ChatAI.Application.Features.Auth.Login;

/// <summary>
/// Command to authenticate an admin user
/// </summary>
public class LoginCommand : IRequest<LoginResult>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
