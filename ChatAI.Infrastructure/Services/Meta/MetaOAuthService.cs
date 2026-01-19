namespace ChatAI.Infrastructure.Services.Meta;

using System.Text.Json;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.MetaOAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements Meta OAuth 2.0 flow for Messenger, Instagram, and WhatsApp channels
/// </summary>
public class MetaOAuthService : IMetaOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaOAuthService> _logger;
    
    private string ClientId => _configuration["Meta:OAuth:ClientId"] 
        ?? throw new InvalidOperationException("Meta OAuth ClientId not configured");
    
    private string ClientSecret => _configuration["Meta:OAuth:ClientSecret"] 
        ?? throw new InvalidOperationException("Meta OAuth ClientSecret not configured");
    
    private string AuthEndpoint => _configuration["Meta:OAuth:AuthorizationEndpoint"]!;
    private string TokenEndpoint => _configuration["Meta:OAuth:TokenEndpoint"]!;
    private string GraphApiVersion => _configuration["Meta:GraphApiVersion"]!;
    
    public MetaOAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MetaOAuthService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MetaGraphApi");
        _configuration = configuration;
        _logger = logger;
    }
    
    public string GenerateAuthorizationUrl(MetaChannel channel, string state, string redirectUri)
    {
        var scopes = GetScopesForChannel(channel);
        var scopeString = string.Join(",", scopes);
        
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["scope"] = scopeString,
            ["response_type"] = "code"
        };
        
        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var authUrl = $"{AuthEndpoint}?{queryString}";
        
        _logger.LogInformation(
            "Generated OAuth authorization URL for channel {Channel} with scopes: {Scopes}", 
            channel, 
            scopeString);
        
        return authUrl;
    }
    
    public async Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        string code, 
        string redirectUri, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exchanging authorization code for access token with redirect_uri: {RedirectUri}", redirectUri);
        
        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["code"] = code
        };
        
        var response = await _httpClient.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(requestBody),
            cancellationToken);
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Token exchange failed: {StatusCode} - {Error}", 
                response.StatusCode, 
                content);
            
            // Parse Meta error response
            try
            {
                using var errorDoc = JsonDocument.Parse(content);
                if (errorDoc.RootElement.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.TryGetProperty("message", out var msg) 
                        ? msg.GetString() 
                        : "Unknown error";
                    var errorCode = error.TryGetProperty("code", out var codeElement) 
                        ? codeElement.GetInt32() 
                        : 0;
                    
                    throw new InvalidOperationException($"Meta OAuth error (code {errorCode}): {errorMessage}");
                }
            }
            catch (JsonException)
            {
                // If error parsing fails, use raw content
            }
            
            throw new InvalidOperationException($"Token exchange failed (HTTP {response.StatusCode}): {content}");
        }
        var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize token response");
        }
        
        _logger.LogInformation(
            "Successfully exchanged code for access token (expires in {ExpiresIn}s, at {ExpiresAt})", 
            tokenResponse.ExpiresIn,
            tokenResponse.ExpiresAt);
        
        return tokenResponse;
    }
    
    public async Task<OAuthTokenResponse> GetLongLivedTokenAsync(
        string shortLivedToken, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extending token to long-lived (60 days)");
        
        var url = $"https://graph.facebook.com/{GraphApiVersion}/oauth/access_token" +
                  $"?grant_type=fb_exchange_token" +
                  $"&client_id={Uri.EscapeDataString(ClientId)}" +
                  $"&client_secret={Uri.EscapeDataString(ClientSecret)}" +
                  $"&fb_exchange_token={Uri.EscapeDataString(shortLivedToken)}";
        
        _logger.LogInformation("Long-lived token exchange URL (token length: {TokenLength})", shortLivedToken?.Length ?? 0);
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Long-lived token exchange failed: {Error}", errorContent);
            throw new InvalidOperationException($"Failed to get long-lived token: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize long-lived token response");
        }
        
        _logger.LogInformation(
            "Successfully extended token to long-lived (expires in {ExpiresIn}s, at {ExpiresAt})", 
            tokenResponse.ExpiresIn,
            tokenResponse.ExpiresAt);
        return tokenResponse;
    }
    
    public async Task<List<MetaAccountInfo>> GetUserPagesAsync(
        string userAccessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user's Facebook Pages with access tokens");
        
        // Get all pages the user manages, including their page-scoped access tokens
        var url = $"https://graph.facebook.com/{GraphApiVersion}/me/accounts" +
                  $"?fields=id,name,category,access_token,picture" +
                  $"&access_token={userAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch user pages: {Error}", errorContent);
            throw new InvalidOperationException($"Failed to fetch user pages: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        
        if (!doc.RootElement.TryGetProperty("data", out var pagesArray))
        {
            throw new InvalidOperationException("No pages found in API response");
        }
        
        var pages = new List<MetaAccountInfo>();
        
        foreach (var page in pagesArray.EnumerateArray())
        {
            var pageInfo = new MetaAccountInfo
            {
                Id = page.GetProperty("id").GetString()!,
                Name = page.GetProperty("name").GetString()!,
                Category = page.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                ProfilePictureUrl = page.TryGetProperty("picture", out var pic) 
                    ? pic.GetProperty("data").GetProperty("url").GetString() 
                    : null,
                // CRITICAL: Store the page-scoped access token, not the user token
                AccessToken = page.GetProperty("access_token").GetString()!,
                Scopes = new List<string> { "pages_messaging", "pages_manage_metadata" },
                IsSystemUser = false
            };
            
            pages.Add(pageInfo);
        }
        
        _logger.LogInformation("Successfully fetched {PageCount} pages for user", pages.Count);
        return pages;
    }
    
    public async Task<MetaAccountInfo> GetPageInfoAsync(
        string userAccessToken, 
        string pageId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching page info for Page ID: {PageId}", pageId);
        
        // Don't request access_token field - requires pages_read_engagement permission
        // Use the provided userAccessToken instead
        var url = $"https://graph.facebook.com/{GraphApiVersion}/{pageId}" +
                  $"?fields=id,name,category,picture" +
                  $"&access_token={userAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch page info for {PageId}: {Error}", pageId, errorContent);
            throw new InvalidOperationException($"Failed to fetch page info: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        var accountInfo = new MetaAccountInfo
        {
            Id = root.GetProperty("id").GetString()!,
            Name = root.GetProperty("name").GetString()!,
            Category = root.TryGetProperty("category", out var cat) ? cat.GetString() : null,
            ProfilePictureUrl = root.TryGetProperty("picture", out var pic) 
                ? pic.GetProperty("data").GetProperty("url").GetString() 
                : null,
            Scopes = new List<string> { "pages_messaging", "pages_manage_metadata" },
            IsSystemUser = false,
            // Use the provided user access token (long-lived)
            AccessToken = userAccessToken
        };
        
        _logger.LogInformation("Successfully fetched page info for {PageName} ({PageId})", accountInfo.Name, pageId);
        return accountInfo;
    }
    
    public async Task<MetaAccountInfo> GetInstagramAccountAsync(
        string userAccessToken, 
        string instagramAccountId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Instagram account info for Account ID: {AccountId}", instagramAccountId);
        
        var url = $"https://graph.facebook.com/{GraphApiVersion}/{instagramAccountId}" +
                  $"?fields=id,username,name,profile_picture_url" +
                  $"&access_token={userAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch Instagram account info for {AccountId}: {Error}", instagramAccountId, errorContent);
            throw new InvalidOperationException($"Failed to fetch Instagram account info: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        var accountInfo = new MetaAccountInfo
        {
            Id = root.GetProperty("id").GetString()!,
            Name = root.GetProperty("username").GetString()!,
            ProfilePictureUrl = root.TryGetProperty("profile_picture_url", out var pic) 
                ? pic.GetString() 
                : null,
            Scopes = new List<string> { "instagram_basic", "instagram_manage_messages" },
            IsSystemUser = false,
            AccessToken = userAccessToken
        };
        
        _logger.LogInformation("Successfully fetched Instagram account info for @{Username} ({AccountId})", accountInfo.Name, instagramAccountId);
        return accountInfo;
    }
    
    public async Task<MetaAccountInfo> GetWhatsAppPhoneAsync(
        string userAccessToken, 
        string phoneNumberId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching WhatsApp phone info for Phone ID: {PhoneId}", phoneNumberId);
        
        var url = $"https://graph.facebook.com/{GraphApiVersion}/{phoneNumberId}" +
                  $"?fields=id,display_phone_number,verified_name" +
                  $"&access_token={userAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch WhatsApp phone info for {PhoneId}: {Error}", phoneNumberId, errorContent);
            throw new InvalidOperationException($"Failed to fetch WhatsApp phone info: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        var accountInfo = new MetaAccountInfo
        {
            Id = root.GetProperty("id").GetString()!,
            Name = root.GetProperty("display_phone_number").GetString()!,
            Scopes = new List<string> { "whatsapp_business_messaging" },
            IsSystemUser = false,
            AccessToken = userAccessToken
        };
        
        _logger.LogInformation("Successfully fetched WhatsApp phone info for {PhoneNumber} ({PhoneId})", accountInfo.Name, phoneNumberId);
        return accountInfo;
    }
    
    public async Task<MetaAccountInfo?> GetPageInstagramAccountAsync(
        string pageAccessToken,
        string pageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Instagram Business Account for Page ID: {PageId}", pageId);
        
        // Fetch Page with instagram_business_account field
        var url = $"https://graph.facebook.com/{GraphApiVersion}/{pageId}" +
                  $"?fields=instagram_business_account{{id,username,name,profile_picture_url}}" +
                  $"&access_token={pageAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch Instagram account for Page {PageId}: {Error}", pageId, errorContent);
            throw new InvalidOperationException($"Failed to fetch Instagram account info: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        // Check if Instagram Business Account is linked
        if (!root.TryGetProperty("instagram_business_account", out var igAccount))
        {
            _logger.LogWarning("No Instagram Business Account linked to Page {PageId}", pageId);
            return null;
        }
        
        var accountInfo = new MetaAccountInfo
        {
            Id = igAccount.GetProperty("id").GetString()!,
            Name = igAccount.TryGetProperty("username", out var username) 
                ? username.GetString()! 
                : igAccount.TryGetProperty("name", out var name) 
                    ? name.GetString()! 
                    : "Instagram Account",
            ProfilePictureUrl = igAccount.TryGetProperty("profile_picture_url", out var pic) 
                ? pic.GetString() 
                : null,
            Scopes = new List<string> { "pages_messaging", "pages_manage_metadata", "pages_show_list" },
            IsSystemUser = false,
            AccessToken = pageAccessToken // Use page token for Instagram API calls
        };
        
        _logger.LogInformation(
            "Successfully fetched Instagram account @{Username} ({AccountId}) for Page {PageId}", 
            accountInfo.Name, 
            accountInfo.Id, 
            pageId);
        
        return accountInfo;
    }
    
    public async Task<List<WhatsAppPhoneInfo>> GetWhatsAppBusinessPhonesAsync(
        string userAccessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching WhatsApp Business Phone Numbers");
        
        // First, get all WhatsApp Business Accounts the user has access to
        var url = $"https://graph.facebook.com/{GraphApiVersion}/me" +
                  $"?fields=businesses{{id,name}}" +
                  $"&access_token={userAccessToken}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch businesses: {Error}", errorContent);
            throw new InvalidOperationException($"Failed to fetch WhatsApp businesses: {errorContent}");
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        
        if (!doc.RootElement.TryGetProperty("businesses", out var businessesProperty) ||
            !businessesProperty.TryGetProperty("data", out var businessesArray) ||
            businessesArray.GetArrayLength() == 0)
        {
            _logger.LogWarning("No businesses found for user");
            return new List<WhatsAppPhoneInfo>();
        }
        
        var allPhones = new List<WhatsAppPhoneInfo>();
        
        // For each business, fetch its WhatsApp Business Accounts
        foreach (var business in businessesArray.EnumerateArray())
        {
            var businessId = business.GetProperty("id").GetString();
            
            // Fetch WhatsApp Business Accounts for this business
            var wabaUrl = $"https://graph.facebook.com/{GraphApiVersion}/{businessId}" +
                         $"?fields=owned_whatsapp_business_accounts{{id,name}}" +
                         $"&access_token={userAccessToken}";
            
            var wabaResponse = await _httpClient.GetAsync(wabaUrl, cancellationToken);
            
            if (!wabaResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch WABAs for business {BusinessId}", businessId);
                continue;
            }
            
            var wabaContent = await wabaResponse.Content.ReadAsStringAsync(cancellationToken);
            using var wabaDoc = JsonDocument.Parse(wabaContent);
            
            if (!wabaDoc.RootElement.TryGetProperty("owned_whatsapp_business_accounts", out var wabaProperty) ||
                !wabaProperty.TryGetProperty("data", out var wabaArray))
            {
                continue;
            }
            
            // For each WABA, fetch its phone numbers
            foreach (var waba in wabaArray.EnumerateArray())
            {
                var wabaId = waba.GetProperty("id").GetString();
                
                var phonesUrl = $"https://graph.facebook.com/{GraphApiVersion}/{wabaId}/phone_numbers" +
                               $"?fields=id,display_phone_number,verified_name,code_verification_status,quality_rating" +
                               $"&access_token={userAccessToken}";
                
                var phonesResponse = await _httpClient.GetAsync(phonesUrl, cancellationToken);
                
                if (!phonesResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch phone numbers for WABA {WabaId}", wabaId);
                    continue;
                }
                
                var phonesContent = await phonesResponse.Content.ReadAsStringAsync(cancellationToken);
                using var phonesDoc = JsonDocument.Parse(phonesContent);
                
                if (!phonesDoc.RootElement.TryGetProperty("data", out var phonesArray))
                {
                    continue;
                }
                
                foreach (var phone in phonesArray.EnumerateArray())
                {
                    var phoneInfo = new WhatsAppPhoneInfo
                    {
                        Id = phone.GetProperty("id").GetString()!,
                        WabaId = wabaId!,
                        DisplayPhoneNumber = phone.GetProperty("display_phone_number").GetString()!,
                        VerifiedName = phone.TryGetProperty("verified_name", out var vn) ? vn.GetString() : null,
                        CodeVerificationStatus = phone.TryGetProperty("code_verification_status", out var cvs) 
                            ? cvs.GetString()! 
                            : "UNVERIFIED",
                        QualityRating = phone.TryGetProperty("quality_rating", out var qr) ? qr.GetString() : null,
                        AccessToken = userAccessToken
                    };
                    
                    allPhones.Add(phoneInfo);
                    
                    _logger.LogInformation(
                        "Found WhatsApp phone: {PhoneNumber} (ID: {PhoneId}, Status: {Status}, Quality: {Quality})",
                        phoneInfo.DisplayPhoneNumber,
                        phoneInfo.Id,
                        phoneInfo.CodeVerificationStatus,
                        phoneInfo.QualityRating ?? "UNKNOWN");
                }
            }
        }
        
        _logger.LogInformation("Successfully fetched {PhoneCount} WhatsApp phone numbers", allPhones.Count);
        return allPhones;
    }
    
    private List<string> GetScopesForChannel(MetaChannel channel)
    {
        var scopeKey = $"Meta:OAuth:Scopes:{channel}";
        var scopes = _configuration.GetSection(scopeKey).Get<List<string>>();
        
        if (scopes == null || scopes.Count == 0)
        {
            throw new InvalidOperationException($"Scopes not configured for channel: {channel}");
        }
        
        return scopes;
    }
}
