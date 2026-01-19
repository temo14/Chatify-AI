using MediatR;

namespace ChatAI.Application.Features.MetaChannels.CreateConnection;

/// <summary>
/// Command to create a new Meta channel connection
/// </summary>
public class CreateMetaConnectionCommand : IRequest<CreateMetaConnectionResult>
{
    public Domain.Enums.MetaChannel Channel { get; set; }
    public string MetaAppId { get; set; } = string.Empty;
    public string MetaAppSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    
    // Channel-specific identifiers
    public string? FacebookPageId { get; set; }
    public string? InstagramBusinessAccountId { get; set; }
    public string? WhatsAppPhoneNumberId { get; set; }
    public string? WhatsAppBusinessAccountId { get; set; }
}

public class CreateMetaConnectionResult
{
    public Guid ConnectionId { get; set; }
    public Guid WebhookId { get; set; }
    public string VerifyToken { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
