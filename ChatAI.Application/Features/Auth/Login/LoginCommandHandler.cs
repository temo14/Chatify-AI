
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
        _logger.LogInformation("Login attempt for user: {Username} at tenant: {Slug}", request.Username, request.Slug);
        
        // STEP 1: Resolve tenant from slug first
        var tenant = await _tenantRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (tenant == null)
        {
            _logger.LogWarning("Login failed: Tenant not found - {Slug}", request.Slug);
            throw new UnauthorizedException("Invalid credentials");
        }
        
        if (!tenant.IsActive)
        {
            _logger.LogWarning("Login failed: Tenant inactive - {Slug}", request.Slug);
            throw new UnauthorizedException("Your organization's account is currently inactive. Please contact support.");
        }
        
        // STEP 2: Get user by username AND tenant ID (prevent cross-tenant username collisions)
        var user = await _userRepository.GetByUsernameAndTenantAsync(request.Username, tenant.Id, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {Username} for tenant {Slug}", request.Username, request.Slug);
            throw new UnauthorizedException("Invalid credentials");
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
