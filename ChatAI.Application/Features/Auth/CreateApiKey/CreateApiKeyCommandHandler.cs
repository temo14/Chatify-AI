
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
        _logger.LogInformation("Creating new API key for client: {ClientName}", request.ClientName);
        
        // Generate API key and client ID
        var (plainKey, keyHash) = _apiKeyService.GenerateApiKey();
        var clientId = _apiKeyService.GenerateClientId();
        
        // Ensure client ID is unique
        while (await _apiKeyRepository.ClientIdExistsAsync(clientId, cancellationToken))
        {
            clientId = _apiKeyService.GenerateClientId();
        }
        
        // Create API key entity
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash,
            ClientName = request.ClientName,
            ClientId = clientId,
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
        
        _logger.LogInformation("API key created successfully: {ClientId}", clientId);
        
        // Return response with plain key (ONLY time it's visible)
        return new ApiKeyResult
        {
            Id = apiKey.Id,
            ClientName = apiKey.ClientName,
            ClientId = apiKey.ClientId,
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
