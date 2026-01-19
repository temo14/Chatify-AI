namespace ChatAI.Domain.Interfaces.Services;

using ChatAI.Domain.Enums;
using ChatAI.Domain.Models.MetaOAuth;

/// <summary>
/// OAuth 2.0 integration service for Meta channels (Messenger, Instagram, WhatsApp)
/// </summary>
public interface IMetaOAuthService
{
    /// <summary>
    /// Generates the authorization URL for Meta OAuth flow
    /// </summary>
    /// <param name="channel">Channel type (Messenger/Instagram/WhatsApp)</param>
    /// <param name="state">CSRF protection state parameter (encrypted)</param>
    /// <param name="redirectUri">OAuth callback URL</param>
    /// <returns>Meta authorization URL</returns>
    string GenerateAuthorizationUrl(MetaChannel channel, string state, string redirectUri);
    
    /// <summary>
    /// Exchanges authorization code for access token
    /// </summary>
    Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        string code, 
        string redirectUri, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extends short-lived token to long-lived token (60 days)
    /// </summary>
    Task<OAuthTokenResponse> GetLongLivedTokenAsync(
        string shortLivedToken, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches user's Facebook Pages with their access tokens
    /// </summary>
    Task<List<MetaAccountInfo>> GetUserPagesAsync(
        string userAccessToken,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches Facebook Page information
    /// </summary>
    Task<MetaAccountInfo> GetPageInfoAsync(
        string accessToken, 
        string pageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches Instagram Business Account information
    /// </summary>
    Task<MetaAccountInfo> GetInstagramAccountAsync(
        string accessToken, 
        string instagramAccountId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches WhatsApp Business Phone Number information
    /// </summary>
    Task<MetaAccountInfo> GetWhatsAppPhoneAsync(
        string accessToken, 
        string phoneNumberId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches Instagram Business Account linked to a Facebook Page
    /// Returns null if no Instagram account is linked
    /// </summary>
    Task<MetaAccountInfo?> GetPageInstagramAccountAsync(
        string pageAccessToken,
        string pageId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches all WhatsApp Business Phone Numbers for a user
    /// Includes verification status for each phone number
    /// </summary>
    Task<List<WhatsAppPhoneInfo>> GetWhatsAppBusinessPhonesAsync(
        string userAccessToken,
        CancellationToken cancellationToken = default);
}
