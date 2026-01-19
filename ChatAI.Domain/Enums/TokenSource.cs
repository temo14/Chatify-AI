namespace ChatAI.Domain.Enums;

/// <summary>
/// How the access token was obtained
/// </summary>
public enum TokenSource
{
    /// <summary>
    /// Token manually entered by admin in UI.
    /// Requires manual rotation every 60 days.
    /// </summary>
    Manual = 0,
    
    /// <summary>
    /// Token obtained via OAuth flow.
    /// Can be automatically refreshed (if refresh token available).
    /// </summary>
    OAuth = 1
}
