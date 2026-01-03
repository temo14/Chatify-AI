using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;

using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Auth.ValidateApiKey;

public class ValidateApiKeyQueryHandler : IRequestHandler<ValidateApiKeyQuery, ApiKey?>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ValidateApiKeyQueryHandler> _logger;
    
    public ValidateApiKeyQueryHandler(
        IApiKeyRepository apiKeyRepository,
        IApiKeyService apiKeyService,
        ILogger<ValidateApiKeyQueryHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }
    
    public async Task<ApiKey?> Handle(ValidateApiKeyQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return null;
        }
        
        // Hash the provided API key
        var keyHash = _apiKeyService.HashApiKey(request.ApiKey);
        
        _logger.LogInformation("Validating API key - Prefix: {Prefix}, Hash: {Hash}", 
            request.ApiKey.Substring(0, Math.Min(15, request.ApiKey.Length)), 
            keyHash);
        
        // Find the key in database
        var apiKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash, cancellationToken);
        
        if (apiKey == null)
        {
            _logger.LogWarning("API key validation failed: Key not found. Hash: {Hash}", keyHash);
            return null;
        }
        
        // Check if key is active
        if (!apiKey.IsActive)
        {
            _logger.LogWarning("API key validation failed: Key inactive - Tenant: {TenantId}", apiKey.TenantId);
            return null;
        }
        
        // Check if key has expired
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("API key validation failed: Key expired - Tenant: {TenantId}", apiKey.TenantId);
            return null;
        }
        
        // Update usage statistics
        apiKey.LastUsedAt = DateTime.UtcNow;
        apiKey.UsageCount++;
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
        
        _logger.LogInformation("API key validated successfully for Tenant: {TenantId}", apiKey.TenantId);
        
        return apiKey;
    }
}
