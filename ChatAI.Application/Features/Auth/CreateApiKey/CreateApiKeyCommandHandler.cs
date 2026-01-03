
using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Auth.CreateApiKey;

public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, ApiKeyResult>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<CreateApiKeyCommandHandler> _logger;
    
    public CreateApiKeyCommandHandler(
        IApiKeyRepository apiKeyRepository,
        IApiKeyService apiKeyService,
        ILogger<CreateApiKeyCommandHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }
    
    public async Task<ApiKeyResult> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new API key for client: {ClientName}, Tenant: {TenantId}", 
            request.ClientName, request.TenantId);
        
        // Generate API key
        var (plainKey, keyHash) = _apiKeyService.GenerateApiKey();
        
        _logger.LogInformation("Generated API key - Prefix: {Prefix}, Hash: {Hash}", 
            plainKey.Substring(0, Math.Min(15, plainKey.Length)), 
            keyHash);
        
        // Create API key entity
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash,
            ClientName = request.ClientName,
            TenantId = request.TenantId.ToString(),
            Description = request.Description,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        // Save to database
        await _apiKeyRepository.CreateAsync(apiKey, cancellationToken);
        
        _logger.LogInformation("API key created successfully for Tenant: {TenantId}", apiKey.TenantId);
        
        // Return response with plain key (ONLY time it's visible)
        return new ApiKeyResult
        {
            Id = apiKey.Id,
            ClientName = apiKey.ClientName,
            TenantId = apiKey.TenantId,
            Description = apiKey.Description,
            IsActive = apiKey.IsActive,
            RateLimitPerMinute = apiKey.RateLimitPerMinute,
            RateLimitPerDay = apiKey.RateLimitPerDay,
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            ApiKey = plainKey // ONLY populated on creation
        };
    }
}
