
using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAdminUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<LoginCommandHandler> _logger;
    
    public LoginCommandHandler(
        IAdminUserRepository userRepository,
        IAuthService authService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }
    
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        // Get user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
            throw new UnauthorizedException("Invalid username or password");
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
        
        _logger.LogInformation("Login successful for user: {Username}", request.Username);
        
        return new LoginResult
        {
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }
}
