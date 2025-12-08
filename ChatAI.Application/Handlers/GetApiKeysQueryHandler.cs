using ChatAI.Application.DTOs;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using MediatR;

namespace ChatAI.Application.Handlers;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, List<ApiKeyResponseDto>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    
    public GetApiKeysQueryHandler(IApiKeyRepository apiKeyRepository)
    {
        _apiKeyRepository = apiKeyRepository;
    }
    
    public async Task<List<ApiKeyResponseDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var apiKeys = await _apiKeyRepository.GetAllAsync(request.IncludeInactive, cancellationToken);
        
        return apiKeys.Select(k => new ApiKeyResponseDto
        {
            Id = k.Id,
            ClientName = k.ClientName,
            ClientId = k.ClientId,
            Description = k.Description,
            IsActive = k.IsActive,
            RateLimitPerMinute = k.RateLimitPerMinute,
            RateLimitPerDay = k.RateLimitPerDay,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            UsageCount = k.UsageCount,
            ApiKey = null // Never return plain key after creation
        }).ToList();
    }
}
