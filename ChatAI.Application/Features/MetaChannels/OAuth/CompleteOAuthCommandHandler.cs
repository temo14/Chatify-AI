namespace ChatAI.Application.Features.MetaChannels.OAuth;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChatAI.Application.Exceptions;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.MetaOAuth;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles OAuth completion after Meta callback
/// Validates state, exchanges code for token, creates connection, subscribes webhook
/// </summary>
public class CompleteOAuthCommandHandler : IRequestHandler<CompleteOAuthCommand, CompleteOAuthResult>
{
    private readonly IMetaOAuthService _oauthService;
    private readonly IMetaChannelConnectionRepository _connectionRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompleteOAuthCommandHandler> _logger;
    
    private string WebhookBaseUrl => _configuration["Meta:WebhookBaseUrl"] 
        ?? throw new InvalidOperationException("Meta WebhookBaseUrl not configured");
    
    public CompleteOAuthCommandHandler(
        IMetaOAuthService oauthService,
        IMetaChannelConnectionRepository connectionRepository,
        IEncryptionService encryptionService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<CompleteOAuthCommandHandler> logger)
    {
        _oauthService = oauthService;
        _connectionRepository = connectionRepository;
        _encryptionService = encryptionService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<CompleteOAuthResult> Handle(
        CompleteOAuthCommand request,
        CancellationToken cancellationToken)
    {
        OAuthState? state = null;
        try
        {
            // Step 1: Validate and retrieve state (with replay protection)
            state = await ValidateAndRetrieveStateAsync(
                request.State,
                request.TenantId,
                request.InitiatingUserId,
                cancellationToken,
                consumeState: true);

            _logger.LogInformation(
                "Processing OAuth callback for tenant {TenantId}, channel {Channel}",
                state.TenantId,
                state.Channel);

            // Step 2: Exchange authorization code for access token
            var tokenResponse = await ExchangeCodeForTokenAsync(
                request.Code,
                request.RedirectUri,
                cancellationToken);

            // Step 3: Get long-lived token (60 days)
            var longLivedToken = await GetLongLivedTokenAsync(
                tokenResponse.AccessToken,
                cancellationToken);

            // Step 4: Fetch account information based on channel
            var accountInfo = await FetchAccountInfoAsync(
                state.Channel,
                longLivedToken.AccessToken,
                cancellationToken);
            
            // Step 4.5: Validate permissions/scopes
            ValidateRequiredPermissions(state.Channel, accountInfo.Scopes);

            // Step 5: Check for existing connection
            await CheckExistingConnectionAsync(
                state.Channel,
                accountInfo.Id,
                cancellationToken);

            // Step 6: Create connection in database
            var connection = await CreateConnectionAsync(
                state.TenantId,
                state.Channel,
                accountInfo,
                longLivedToken,
                cancellationToken);

            // Note: Webhook is configured once at Meta App level (/api/webhooks/meta)
            // No per-connection webhook subscription needed in shared app model

            _logger.LogInformation(
                "OAuth flow completed successfully for tenant {TenantId}, channel {Channel}, connection {ConnectionId}",
                state.TenantId,
                state.Channel,
                connection.Id);

            return new CompleteOAuthResult
            {
                Success = true,
                ConnectionId = connection.Id,
                WebhookId = connection.WebhookId,
                VerifyToken = connection.VerifyTokenPlain,
                Channel = connection.Channel
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "OAuth validation failed: {Message}",
                ex.Message);
            return new CompleteOAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "VALIDATION_FAILED"
            };
        }
        catch (DuplicateConnectionException ex)
        {
            _logger.LogWarning(
                "Duplicate connection attempt for channel {Channel}: {Message}",
                state?.Channel,
                ex.Message);
            return new CompleteOAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "DUPLICATE_CONNECTION"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "OAuth flow failed for tenant {TenantId}: {Message}",
                state?.TenantId,
                ex.Message);
            return new CompleteOAuthResult
            {
                Success = false,
                ErrorMessage = "OAuth flow failed. Please try again.",
                ErrorCode = "OAUTH_FAILED"
            };
        }
    }
    
    /// <summary>
    /// Validates state parameter and retrieves original OAuth state from cache
    /// </summary>
    private async Task<OAuthState> ValidateAndRetrieveStateAsync(
        string stateToken,
        Guid requestTenantId,
        string requestInitiatingUserId,
        CancellationToken cancellationToken,
        bool consumeState = false)
    {
        // Validate state format
        if (string.IsNullOrWhiteSpace(stateToken) || stateToken.Length > 500)
        {
            throw new ValidationException("Invalid state parameter");
        }

        if (!TryParseStateToken(stateToken, out var transactionId, out var nonce, out var signature))
        {
            throw new ValidationException("Invalid state parameter");
        }
        
        // Retrieve from cache
        var cacheKey = $"oauth_tx:{transactionId:N}";
        var stateJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(stateJson))
        {
            throw new ValidationException("OAuth state expired, already used, or invalid. Please try again.");
        }
        if (consumeState)
        {
            // Remove state from cache immediately to prevent replay
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }
        // Deserialize state
        var state = JsonSerializer.Deserialize<OAuthState>(stateJson);
        if (state == null)
        {
            throw new ValidationException("Failed to parse OAuth state");
        }
        
        // Validate timestamp (state should not be older than 10 minutes)
        var stateAge = DateTime.UtcNow - state.Timestamp;
        if (stateAge.TotalMinutes > 10)
        {
            throw new ValidationException("OAuth state expired. Please try again.");
        }

        // Ensure nonce matches what was signed (prevents substitution)
        if (!string.Equals(state.Nonce, nonce, StringComparison.Ordinal))
        {
            throw new ValidationException("OAuth state expired or invalid. Please try again.");
        }

        // Verify state signature (binds to txId+nonce+tenant+user+channel+timestamp)
        if (!VerifyStateSignature(state, signature))
        {
            throw new ValidationException("OAuth state expired or invalid. Please try again.");
        }

        // Enforce tenant/user binding (authenticated completion)
        if (state.TenantId != requestTenantId || !string.Equals(state.InitiatingUserId, requestInitiatingUserId, StringComparison.Ordinal))
        {
            throw new ValidationException("OAuth state expired or invalid. Please try again.");
        }
        
        _logger.LogInformation(
            "OAuth state validated successfully for tenant {TenantId}, channel {Channel}",
            state.TenantId,
            state.Channel);
        
        return state;
    }

    private static bool TryParseStateToken(
        string stateToken,
        out Guid transactionId,
        out string nonce,
        out string signature)
    {
        transactionId = default;
        nonce = string.Empty;
        signature = string.Empty;

        // Expected format: {txId}.{nonce}.{sig}
        var parts = stateToken.Split('.', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[0], "N", out transactionId))
        {
            return false;
        }

        nonce = parts[1];
        signature = parts[2];

        if (nonce.Length < 16 || nonce.Length > 200)
        {
            return false;
        }

        if (signature.Length < 16 || signature.Length > 256)
        {
            return false;
        }

        return true;
    }

    private bool VerifyStateSignature(OAuthState state, string providedSignature)
    {
        // StateSigningSecret MUST be provided via Key Vault in production.
        var secret = _configuration["Meta:OAuth:StateSigningSecret"]
            ?? throw new InvalidOperationException("Meta:OAuth:StateSigningSecret not configured");

        var payload = $"{state.TransactionId:N}|{state.Nonce}|{state.TenantId:N}|{state.InitiatingUserId}|{state.Channel}|{state.Timestamp:o}";
        var expectedSignature = ComputeHmacSha256(secret, payload);

        try
        {
            var expectedBytes = Base64UrlDecode(expectedSignature);
            var providedBytes = Base64UrlDecode(providedSignature);
            return expectedBytes.Length == providedBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeHmacSha256(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = hmac.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
    
    /// <summary>
    /// Exchanges authorization code for short-lived access token
    /// </summary>
    private async Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        // Validate code
        if (string.IsNullOrWhiteSpace(code) || code.Length > 1000)
        {
            throw new ValidationException("Invalid authorization code");
        }
        
        _logger.LogInformation("Exchanging authorization code for access token");
        
        try
        {
            return await _oauthService.ExchangeCodeForTokenAsync(
                code,
                redirectUri,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token exchange failed: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to exchange authorization code. Please try again.", ex);
        }
    }
    
    /// <summary>
    /// Extends short-lived token to long-lived token (60 days)
    /// </summary>
    private async Task<OAuthTokenResponse> GetLongLivedTokenAsync(
        string shortLivedToken,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extending token to long-lived");
        
        try
        {
            return await _oauthService.GetLongLivedTokenAsync(
                shortLivedToken,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Long-lived token exchange failed: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to extend token. Please try again.", ex);
        }
    }
    
    /// <summary>
    /// Fetches account information based on channel type
    /// For Messenger: Fetches user's pages with page access tokens
    /// For Instagram/WhatsApp: Fetches account details
    /// </summary>
    private async Task<MetaAccountInfo> FetchAccountInfoAsync(
        MetaChannel channel,
        string accessToken,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching account information for channel {Channel}", channel);
        
        try
        {
            return channel switch
            {
                MetaChannel.Messenger => await FetchMessengerPageAsync(accessToken, cancellationToken),
                MetaChannel.Instagram => await FetchInstagramAccountAsync(accessToken, cancellationToken),
                MetaChannel.WhatsApp => await FetchWhatsAppAccountAsync(accessToken, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported channel: {channel}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch account information for channel {Channel}", channel);
            throw new InvalidOperationException($"Failed to fetch {channel} account information. Please try again.", ex);
        }
    }
    
    /// <summary>
    /// Fetches user's Facebook Pages with page-scoped access tokens
    /// CRITICAL: Returns page access token, not user access token
    /// </summary>
    private async Task<MetaAccountInfo> FetchMessengerPageAsync(
        string userAccessToken,
        CancellationToken cancellationToken)
    {
        var pages = await _oauthService.GetUserPagesAsync(userAccessToken, cancellationToken);
        
        if (pages.Count == 0)
        {
            throw new ValidationException("No Facebook Pages found. You must be an admin of at least one page.");
        }
        
        // Not implemented: multi-page selection — connects first available page
        var selectedPage = pages.First();
        
        _logger.LogInformation(
            "Selected Facebook Page: {PageName} ({PageId})",
            selectedPage.Name,
            selectedPage.Id);
        
        return selectedPage;
    }
    
    /// <summary>
    /// Fetches Instagram Business Account information
    /// </summary>
    private async Task<MetaAccountInfo> FetchInstagramAccountAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        // Step 1: Get the user's Facebook Pages (which includes page access tokens)
        var pages = await _oauthService.GetUserPagesAsync(accessToken, cancellationToken);
        
        if (pages.Count == 0)
        {
            throw new ValidationException(
                "No Facebook Pages found. Instagram must be connected to a Facebook Page. " +
                "Please create a Facebook Page and link your Instagram Business Account to it.");
        }
        
        // Step 2: For each page, check if it has a linked Instagram Business Account
        MetaAccountInfo? instagramAccount = null;
        string? connectedPageId = null;
        
        foreach (var page in pages)
        {
            var igAccount = await _oauthService.GetPageInstagramAccountAsync(
                page.AccessToken!, 
                page.Id, 
                cancellationToken);
            
            if (igAccount != null)
            {
                instagramAccount = igAccount;
                connectedPageId = page.Id;
                _logger.LogInformation(
                    "Found Instagram account @{Username} ({IgId}) linked to Page {PageName} ({PageId})",
                    igAccount.Name,
                    igAccount.Id,
                    page.Name,
                    page.Id);
                break; // Use the first page with an Instagram account
            }
        }
        
        // Step 3: Validate that at least one page has an Instagram Business Account
        if (instagramAccount == null)
        {
            throw new ValidationException(
                "No Instagram Business Account found linked to your Facebook Pages. " +
                "Please link your Instagram Business Account to a Facebook Page in Meta Business Suite, then try again.");
        }
        
        _logger.LogInformation(
            "Successfully retrieved Instagram account @{Username} for OAuth completion",
            instagramAccount.Name);
        
        return instagramAccount;
    }
    
    /// <summary>
    /// Fetches WhatsApp Business Phone Number information
    /// </summary>
    private async Task<MetaAccountInfo> FetchWhatsAppAccountAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        // Step 1: Fetch all WhatsApp Business Phone Numbers accessible to this user
        var phoneNumbers = await _oauthService.GetWhatsAppBusinessPhonesAsync(accessToken, cancellationToken);
        
        if (phoneNumbers.Count == 0)
        {
            throw new ValidationException(
                "No WhatsApp Business Phone Numbers found. " +
                "Please set up a WhatsApp Business Account and add a phone number in Meta Business Manager.");
        }
        
        // Step 2: Filter for verified phone numbers only
        var verifiedPhones = phoneNumbers.Where(p => p.IsVerified).ToList();
        
        if (verifiedPhones.Count == 0)
        {
            throw new ValidationException(
                "No verified WhatsApp phone numbers found. " +
                $"You have {phoneNumbers.Count} phone number(s), but none are verified. " +
                "Please verify your WhatsApp Business phone number in Meta Business Manager before connecting.");
        }
        
        // Not implemented: multi-phone selection — connects first verified phone
        var selectedPhone = verifiedPhones.First();
        
        _logger.LogInformation(
            "Selected WhatsApp phone {PhoneNumber} (ID: {PhoneId}, WABA: {WabaId}, Quality: {Quality})",
            selectedPhone.DisplayPhoneNumber,
            selectedPhone.Id,
            selectedPhone.WabaId,
            selectedPhone.QualityRating ?? "UNKNOWN");
        
        // Step 4: Create account info for connection storage
        var accountInfo = new MetaAccountInfo
        {
            Id = selectedPhone.Id,
            Name = selectedPhone.VerifiedName ?? selectedPhone.DisplayPhoneNumber,
            Category = selectedPhone.WabaId, // Store WABA ID in Category field
            Scopes = new List<string> { "whatsapp_business_messaging", "whatsapp_business_management" },
            IsSystemUser = false,
            AccessToken = accessToken
        };
        
        _logger.LogInformation(
            "Successfully retrieved WhatsApp phone {PhoneNumber} for OAuth completion",
            selectedPhone.DisplayPhoneNumber);
        
        return accountInfo;
    }
    
    /// <summary>
    /// Validates that all required permissions/scopes are granted for the channel
    /// </summary>
    private void ValidateRequiredPermissions(MetaChannel channel, List<string> grantedScopes)
    {
        var requiredScopes = channel switch
        {
            MetaChannel.Messenger => new[] { "pages_messaging", "pages_manage_metadata" },
            MetaChannel.Instagram => new[] { "pages_messaging", "pages_manage_metadata", "pages_show_list" },
            MetaChannel.WhatsApp => new[] { "whatsapp_business_messaging", "whatsapp_business_management" },
            _ => Array.Empty<string>()
        };
        
        var missingScopes = requiredScopes
            .Where(required => !grantedScopes.Any(granted => 
                granted.Equals(required, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        if (missingScopes.Any())
        {
            var missingList = string.Join(", ", missingScopes);
            throw new ValidationException(
                $"Required permissions not granted for {channel}. Missing: {missingList}. " +
                "Please try again and grant all requested permissions.");
        }
        
        _logger.LogInformation(
            "Permission validation passed for {Channel}. Granted scopes: {Scopes}",
            channel,
            string.Join(", ", grantedScopes));
    }
    
    /// <summary>
    /// Checks if connection already exists for this channel identity
    /// Prevents duplicate connections
    /// </summary>
    private async Task CheckExistingConnectionAsync(
        MetaChannel channel,
        string accountId,
        CancellationToken cancellationToken)
    {
        bool exists = channel switch
        {
            MetaChannel.Messenger => await _connectionRepository.ChannelIdentityExistsAsync(
                MetaChannel.Messenger,
                accountId,
                cancellationToken),
            
            MetaChannel.Instagram => await _connectionRepository.ChannelIdentityExistsAsync(
                MetaChannel.Instagram,
                accountId,
                cancellationToken),
            
            MetaChannel.WhatsApp => await _connectionRepository.ChannelIdentityExistsAsync(
                MetaChannel.WhatsApp,
                accountId,
                cancellationToken),
            
            _ => throw new InvalidOperationException($"Unsupported channel: {channel}")
        };
        
        if (exists)
        {
            throw new DuplicateConnectionException($"This {channel} account is already connected");
        }
    }
    
    /// <summary>
    /// Creates connection record in database with encrypted tokens
    /// </summary>
    private async Task<MetaChannelConnection> CreateConnectionAsync(
        Guid tenantId,
        MetaChannel channel,
        MetaAccountInfo accountInfo,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating connection for tenant {TenantId}, channel {Channel}, account {AccountId}",
            tenantId,
            channel,
            accountInfo.Id);
        
        // Generate verify token for webhook validation
        var verifyToken = GenerateVerifyToken();
        var verifyTokenHash = HashVerifyToken(verifyToken);
        
        // Get OAuth ClientId and ClientSecret from configuration
        var oauthClientId = _configuration["Meta:OAuth:ClientId"] 
            ?? throw new InvalidOperationException("Meta OAuth ClientId not configured");
        var oauthClientSecret = _configuration["Meta:OAuth:ClientSecret"] 
            ?? throw new InvalidOperationException("Meta OAuth ClientSecret not configured");
        
        // Create connection entity
        var connection = new MetaChannelConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Channel = channel,
            WebhookId = Guid.NewGuid(),
            VerifyTokenHash = verifyTokenHash,
            VerifyTokenPlain = verifyToken, // Store temporarily for display to user
            MetaAppId = oauthClientId,
            MetaAppSecretEncrypted = _encryptionService.Encrypt(oauthClientSecret ?? "", keyVersion: 1),
            AccessTokenEncrypted = _encryptionService.Encrypt(accountInfo.AccessToken ?? "", keyVersion: 1),
            TokenKeyVersion = 1,
            TokenSource = TokenSource.OAuth,
            OAuthScopes = JsonSerializer.Serialize(accountInfo.Scopes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set channel-specific fields
        switch (channel)
        {
            case MetaChannel.Messenger:
                connection.FacebookPageId = accountInfo.Id;
                break;
            
            case MetaChannel.Instagram:
                connection.InstagramBusinessAccountId = accountInfo.Id;
                break;
            
            case MetaChannel.WhatsApp:
                connection.WhatsAppPhoneNumberId = accountInfo.Id;
                connection.WhatsAppBusinessAccountId = accountInfo.Category; // WABA ID stored in Category
                break;
            
            default:
                throw new InvalidOperationException($"Unsupported channel: {channel}");
        }
        
        // Set token expiration
        connection.TokenExpiresAt = tokenResponse.ExpiresAt;
        
        // Save to database
        var createdConnection = await _connectionRepository.CreateAsync(connection, cancellationToken);
        
        _logger.LogInformation(
            "Connection created successfully: {ConnectionId}, WebhookId: {WebhookId}",
            createdConnection.Id,
            createdConnection.WebhookId);
        
        return createdConnection;
    }
    
    /// <summary>
    /// Attempts to cleanup OAuth state from cache
    /// Failure is logged but doesn't fail the OAuth flow
    /// </summary>
    private async Task TryCleanupStateAsync(
        string stateToken,
        CancellationToken cancellationToken)
    {
        try
        {
            if (TryParseStateToken(stateToken, out var transactionId, out _, out _))
            {
                var cacheKey = $"oauth_tx:{transactionId:N}";
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                _logger.LogInformation("OAuth state cleaned up from cache: {CacheKey}", cacheKey);
                return;
            }

            // Backward-compat (older state format)
            var legacyCacheKey = $"oauth_state:{stateToken}";
            await _cache.RemoveAsync(legacyCacheKey, cancellationToken);
            _logger.LogInformation("OAuth state cleaned up from cache: {CacheKey}", legacyCacheKey);
            
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup OAuth state from cache");
        }
    }
    
    /// <summary>
    /// Generates secure random verify token for webhook validation
    /// </summary>
    private static string GenerateVerifyToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
    
    /// <summary>
    /// Hashes verify token using SHA256 for database storage
    /// </summary>
    private static string HashVerifyToken(string verifyToken)
    {
        var bytes = Encoding.UTF8.GetBytes(verifyToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Exception thrown when duplicate connection is detected
/// </summary>
public class DuplicateConnectionException : Exception
{
    public DuplicateConnectionException(string message) : base(message) { }
}
