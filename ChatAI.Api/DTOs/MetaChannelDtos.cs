using ChatAI.Domain.Enums;

namespace ChatAI.Api.DTOs;

/// <summary>
/// Request to create a new Meta channel connection
/// </summary>
public class CreateMetaConnectionDto
{
    public MetaChannel Channel { get; set; }
    public string MetaAppId { get; set; } = string.Empty;
    public string MetaAppSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    
    // Channel-specific identifiers (provide based on channel type)
    public string? FacebookPageId { get; set; }
    public string? InstagramBusinessAccountId { get; set; }
    public string? WhatsAppPhoneNumberId { get; set; }
    public string? WhatsAppBusinessAccountId { get; set; }
}

/// <summary>
/// Response after creating a Meta channel connection
/// </summary>
public class MetaConnectionResponseDto
{
    public Guid Id { get; set; }
    public MetaChannel Channel { get; set; }
    public Guid WebhookId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string VerifyToken { get; set; } = string.Empty; // Shown once during setup
    public string MetaAppId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastWebhookAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public DateTime? LastSendAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public int FailedSendCount { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool TokenExpiryWarning { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Channel-specific identifiers
    public string? FacebookPageId { get; set; }
    public string? InstagramBusinessAccountId { get; set; }
    public string? WhatsAppPhoneNumberId { get; set; }
}

/// <summary>
/// Request to rotate an access token
/// </summary>
public class RotateMetaTokenDto
{
    public string NewAccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Result of validating a connection
/// </summary>
public class MetaConnectionValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public List<string> Scopes { get; set; } = new();
}
