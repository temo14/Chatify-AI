
using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAdminUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<LoginCommandHandler> _logger;
    
    public LoginCommandHandler(
        IAdminUserRepository userRepository,
        ITenantRepository tenantRepository,
        IAuthService authService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _authService = authService;
        _logger = logger;
    }
    
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        // STEP 1: Get user by username (usernames are globally unique)
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
            throw new UnauthorizedException("Invalid credentials");
        }
        
        // STEP 2: Get tenant from user and verify it's active
        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant == null || !tenant.IsActive)
        {
            _logger.LogWarning("Login failed: Tenant inactive or not found for user {Username}", request.Username);
            throw new UnauthorizedException("Your organization's account is currently inactive. Please contact support.");
        }
        
        // Check if account is locked
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login failed: Account locked until {LockedUntil} - {Username}", 
                user.LockedUntil.Value, request.Username);
            throw new UnauthorizedException($"Account is locked until {user.LockedUntil.Value:g}");
        }
        
        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account inactive - {Username}", request.Username);
            throw new UnauthorizedException("Account is inactive");
        }
        
        // Verify password
        if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;
            
            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account locked after 5 failed login attempts - {Username}", request.Username);
            }
            
            await _userRepository.UpdateAsync(user, cancellationToken);
            
            _logger.LogWarning("Login failed: Invalid password - {Username}", request.Username);
            throw new UnauthorizedException("Invalid username or password");
        }
        
        // Successful login - reset failed attempts and update last login
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);
        
        // Generate JWT token
        var token = _authService.GenerateJwtToken(user, request.RememberMe);
        var expirationMinutes = request.RememberMe ? 10080 : 60; // 7 days or 1 hour
        
        // Determine role: PlatformAdmin (Dott staff) or TenantAdmin (customer)
        var role = user.IsPlatformAdmin ? "PlatformAdmin" : "TenantAdmin";
        
        _logger.LogInformation("Login successful for user: {Username}", request.Username);
        
        return new LoginResult
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            TenantId = user.TenantId, // Multi-tenancy support
            Role = role // PlatformAdmin or TenantAdmin
        };
    }
}
