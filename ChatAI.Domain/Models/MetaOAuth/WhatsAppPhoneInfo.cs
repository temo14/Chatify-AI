namespace ChatAI.Domain.Models.MetaOAuth;

/// <summary>
/// WhatsApp Business Phone Number information
/// Returned from WhatsApp Business Account endpoints
/// </summary>
public record WhatsAppPhoneInfo
{
    /// <summary>
    /// Phone Number ID (unique identifier for WhatsApp Cloud API)
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// WhatsApp Business Account ID (WABA) that owns this phone number
    /// </summary>
    public string WabaId { get; init; } = string.Empty;
    
    /// <summary>
    /// Display phone number (formatted, e.g., "+1 650-555-1234")
    /// </summary>
    public string DisplayPhoneNumber { get; init; } = string.Empty;
    
    /// <summary>
    /// Verified name (business name associated with the phone number)
    /// </summary>
    public string? VerifiedName { get; init; }
    
    /// <summary>
    /// Code verification status (e.g., "VERIFIED", "UNVERIFIED", "PENDING")
    /// </summary>
    public string CodeVerificationStatus { get; init; } = "UNVERIFIED";
    
    /// <summary>
    /// Quality rating (e.g., "GREEN", "YELLOW", "RED", "UNKNOWN")
    /// </summary>
    public string? QualityRating { get; init; }
    
    /// <summary>
    /// Whether the phone number is verified and ready to send messages
    /// </summary>
    public bool IsVerified => CodeVerificationStatus?.Equals("VERIFIED", StringComparison.OrdinalIgnoreCase) == true;
    
    /// <summary>
    /// Access token for this phone number (from OAuth flow)
    /// </summary>
    public string? AccessToken { get; init; }
}
