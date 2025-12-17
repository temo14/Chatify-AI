
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Auth.InitializeDefaultAdmin;

public class InitializeDefaultAdminCommandHandler : IRequestHandler<InitializeDefaultAdminCommand, bool>
{
    private readonly IAdminUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<InitializeDefaultAdminCommandHandler> _logger;
    
    public InitializeDefaultAdminCommandHandler(
        IAdminUserRepository userRepository,
        IAuthService authService,
        ILogger<InitializeDefaultAdminCommandHandler> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }
    
    public async Task<bool> Handle(InitializeDefaultAdminCommand request, CancellationToken cancellationToken)
    {
        // Check if admin user already exists
        var existingAdmin = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        
        if (existingAdmin != null)
        {
            _logger.LogInformation("Default admin user already exists, skipping initialization");
            return false;
        }
        
        // Create default admin user
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _authService.HashPassword(request.Password),
            Email = request.Email,
            FullName = "System Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.CreateAsync(adminUser, cancellationToken);
        
        _logger.LogInformation("Default admin user created successfully: {Username}", request.Username);
        
        return true;
    }
}
