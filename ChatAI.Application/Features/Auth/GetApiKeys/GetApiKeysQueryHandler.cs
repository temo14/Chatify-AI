using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Features.Auth.GetApiKeys;

public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, List<ApiKey>>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ITenantContext _tenantContext;
    
    public GetApiKeysQueryHandler(
        IApiKeyRepository apiKeyRepository,
        ITenantContext tenantContext)
    {
        _apiKeyRepository = apiKeyRepository;
        _tenantContext = tenantContext;
    }
    
    public async Task<List<ApiKey>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
    {
        var apiKeys = await _apiKeyRepository.GetAllAsync(request.IncludeInactive, cancellationToken);
        
        // Filter by current tenant
        var tenantId = _tenantContext.RequiredTenantId.ToString();
        return apiKeys.Where(k => k.TenantId == tenantId).ToList();
    }
}
