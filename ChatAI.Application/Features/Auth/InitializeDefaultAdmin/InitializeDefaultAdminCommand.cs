using MediatR;

namespace ChatAI.Application.Features.Auth.InitializeDefaultAdmin;

/// <summary>
/// Command to initialize default admin user if none exists
/// </summary>
public class InitializeDefaultAdminCommand : IRequest<bool>
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Admin@123456";
    public string? Email { get; set; }
}
