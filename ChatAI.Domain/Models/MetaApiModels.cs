namespace ChatAI.Domain.Models;

/// <summary>
/// Result of sending a message via Meta Graph API
/// </summary>
public class MetaSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldDisableConnection { get; set; } // True for token errors (190, 200)
    public bool WasTruncated { get; set; } // True if message was truncated due to length
    public int? OriginalLength { get; set; }
    public int? TruncatedLength { get; set; }
}

/// <summary>
/// Result of validating a Meta access token
/// </summary>
public class MetaTokenValidationResult
{
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string? AppId { get; set; }
}
