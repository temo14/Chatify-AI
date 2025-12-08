using ChatAI.Application.Commands;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, bool>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<RevokeApiKeyCommandHandler> _logger;
    
    public RevokeApiKeyCommandHandler(
        IApiKeyRepository apiKeyRepository,
        ILogger<RevokeApiKeyCommandHandler> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _logger = logger;
    }
    
    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking API key: {KeyId}", request.KeyId);
        
        var apiKey = await _apiKeyRepository.GetByIdAsync(request.KeyId, cancellationToken);
        
        if (apiKey == null)
        {
            throw new NotFoundException($"API key with ID {request.KeyId} not found");
        }
        
        // Deactivate the key
        apiKey.IsActive = false;
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
        
        _logger.LogInformation("API key revoked successfully: {ClientId}", apiKey.ClientId);
        
        return true;
    }
}
