namespace ChatAI.Api.DTOs.MetaOAuth;

using ChatAI.Domain.Enums;

/// <summary>
/// Request to initiate OAuth flow for a Meta channel
/// </summary>
public record OAuthInitiateRequestDto
{
    /// <summary>
    /// Channel to connect (Messenger/Instagram/WhatsApp)
    /// </summary>
    public MetaChannel Channel { get; init; }
}
