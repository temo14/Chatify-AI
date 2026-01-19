namespace ChatAI.Application.Features.MetaChannels.OAuth;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChatAI.Domain.Models.MetaOAuth;
using ChatAI.Domain.Interfaces;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles OAuth initiation by generating authorization URL and storing encrypted state
/// </summary>
public class InitiateOAuthCommandHandler : IRequestHandler<InitiateOAuthCommand, InitiateOAuthResult>
{
    private readonly IMetaOAuthService _oauthService;
    private readonly IEncryptionService _encryptionService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InitiateOAuthCommandHandler> _logger;
    
    public InitiateOAuthCommandHandler(
        IMetaOAuthService oauthService,
        IEncryptionService encryptionService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<InitiateOAuthCommandHandler> logger)
    {
        _oauthService = oauthService;
        _encryptionService = encryptionService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<InitiateOAuthResult> Handle(
        InitiateOAuthCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Initiating OAuth flow for tenant {TenantId}, channel {Channel}",
                request.TenantId,
                request.Channel);
            
            if (string.IsNullOrWhiteSpace(request.InitiatingUserId))
            {
                return new InitiateOAuthResult
                {
                    Success = false,
                    ErrorMessage = "Missing initiating user context"
                };
            }

            // Create server-side OAuth transaction state (binds completion to tenant+user)
            var state = new OAuthState
            {
                TransactionId = Guid.NewGuid(),
                TenantId = request.TenantId,
                InitiatingUserId = request.InitiatingUserId,
                Channel = request.Channel,
                Timestamp = DateTime.UtcNow,
                Nonce = GenerateNonce()
            };

            var stateJson = JsonSerializer.Serialize(state);

            // Store transaction in distributed cache (10 minute expiration)
            var cacheKey = $"oauth_tx:{state.TransactionId:N}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            await _cache.SetStringAsync(cacheKey, stateJson, cacheOptions, cancellationToken);
            
            _logger.LogInformation(
                "Stored OAuth state in cache with key: {CacheKey} (expires in 10 minutes)",
                cacheKey);
            
            // Generate OAuth authorization URL
            var redirectUri = _configuration["Meta:OAuth:RedirectUri"] 
                ?? throw new InvalidOperationException("Meta OAuth RedirectUri not configured");
            
            var stateToken = CreateSignedStateToken(state);

            var authorizationUrl = _oauthService.GenerateAuthorizationUrl(
                request.Channel,
                stateToken,
                redirectUri);
            
            _logger.LogInformation(
                "OAuth flow initiated successfully for tenant {TenantId}, channel {Channel}",
                request.TenantId,
                request.Channel);
            
            return new InitiateOAuthResult
            {
                Success = true,
                AuthorizationUrl = authorizationUrl,
                State = stateToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to initiate OAuth flow for tenant {TenantId}, channel {Channel}", 
                request.TenantId,
                request.Channel);
            
            return new InitiateOAuthResult
            {
                Success = false,
                ErrorMessage = "Failed to initiate OAuth flow. Please try again."
            };
        }
    }
    
    private static string GenerateNonce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private string CreateSignedStateToken(OAuthState state)
    {
        // StateSigningSecret MUST be provided via Key Vault in production.
        var secret = _configuration["Meta:OAuth:StateSigningSecret"]
            ?? throw new InvalidOperationException("Meta:OAuth:StateSigningSecret not configured");

        var payload = $"{state.TransactionId:N}|{state.Nonce}|{state.TenantId:N}|{state.InitiatingUserId}|{state.Channel}|{state.Timestamp:o}";
        var signature = ComputeHmacSha256(secret, payload);

        // state format: {txId}.{nonce}.{sig}
        return $"{state.TransactionId:N}.{state.Nonce}.{signature}";
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
}
