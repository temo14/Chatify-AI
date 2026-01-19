namespace ChatAI.Application.Features.MetaChannels.OAuth;

using ChatAI.Domain.Enums;
using MediatR;

/// <summary>
/// Command to complete Meta OAuth flow after callback
/// Validates state, exchanges code for token, creates connection
/// </summary>
public record CompleteOAuthCommand(
    Guid TenantId,
    string InitiatingUserId,
    string Code,
    string State,
    string RedirectUri
) : IRequest<CompleteOAuthResult>;

/// <summary>
/// Result of OAuth completion
/// </summary>
public record CompleteOAuthResult
{
    public bool Success { get; init; }
    public Guid? ConnectionId { get; init; }
    public Guid? WebhookId { get; init; }
    public string? VerifyToken { get; init; }
    public MetaChannel? Channel { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}
