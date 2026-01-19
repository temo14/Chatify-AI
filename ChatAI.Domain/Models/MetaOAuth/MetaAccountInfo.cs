namespace ChatAI.Domain.Models.MetaOAuth;

/// <summary>
/// Account metadata fetched after OAuth authorization
/// </summary>
public record MetaAccountInfo
{
    /// <summary>
    /// Account ID (Page ID / Instagram Account ID / Phone Number ID)
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Account name or username
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Account category (for Pages)
    /// For WhatsApp: stores WABA ID
    /// </summary>
    public string? Category { get; init; }
    
    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; init; }
    
    /// <summary>
    /// OAuth scopes granted by user
    /// </summary>
    public List<string> Scopes { get; init; } = new();
    
    /// <summary>
    /// Whether this is a System User token (never expires)
    /// </summary>
    public bool IsSystemUser { get; init; }
    
    /// <summary>
    /// Page access token (for Messenger)
    /// </summary>
    public string? AccessToken { get; init; }
}
