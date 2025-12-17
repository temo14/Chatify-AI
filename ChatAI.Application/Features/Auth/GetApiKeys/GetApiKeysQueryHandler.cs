using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Features.Auth.GetApiKeys;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, List<ApiKey>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    
    public GetApiKeysQueryHandler(IApiKeyRepository apiKeyRepository)
    {
        _apiKeyRepository = apiKeyRepository;
    }
    
    public async Task<List<ApiKey>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var apiKeys = await _apiKeyRepository.GetAllAsync(request.IncludeInactive, cancellationToken);
        return apiKeys.ToList();
    }
}
