namespace ChatAI.Application.Configuration;

/// <summary>
/// JWT configuration options
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// Secret key for signing JWT tokens
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = "ChatifyAI";
    
    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = "ChatifyAI";
    
    /// <summary>
    /// Token expiration in minutes (default session)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Token expiration in minutes (remember me)
    /// </summary>
    public int RememberMeExpirationMinutes { get; set; } = 10080; // 7 days
}
