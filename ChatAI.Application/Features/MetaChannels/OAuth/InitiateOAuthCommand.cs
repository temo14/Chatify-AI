namespace ChatAI.Application.Features.MetaChannels.OAuth;

using ChatAI.Domain.Enums;
using MediatR;

/// <summary>
/// Command to initiate Meta OAuth flow for a tenant
/// Generates authorization URL and stores encrypted state in cache
/// </summary>
public record InitiateOAuthCommand(
    Guid TenantId,
    string InitiatingUserId,
    MetaChannel Channel
) : IRequest<InitiateOAuthResult>;

/// <summary>
/// Result of OAuth initiation
/// </summary>
public record InitiateOAuthResult
{
    public bool Success { get; init; }
    public string? AuthorizationUrl { get; init; }
    public string? State { get; init; }
    public string? ErrorMessage { get; init; }
}
