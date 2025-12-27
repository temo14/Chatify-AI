using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.AdminUsers.ResetPassword;

public class ResetUserPasswordCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long");
    }
}

public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, Unit>
{
    private readonly IAdminUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<ResetUserPasswordCommandHandler> _logger;

    public ResetUserPasswordCommandHandler(
        IAdminUserRepository userRepository,
        IAuthService authService,
        ILogger<ResetUserPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException($"User with ID {request.UserId} not found");
        }

        // Hash the new password
        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        
        // Reset login attempts and unlock if locked
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password reset for user {UserId} ({Username})", user.Id, user.Username);

        return Unit.Value;
    }
}
