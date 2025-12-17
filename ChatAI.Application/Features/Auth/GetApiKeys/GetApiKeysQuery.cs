using MediatR;
using ChatAI.Domain.Entities;

namespace ChatAI.Application.Features.Auth.GetApiKeys;

/// <summary>
/// Query to get all API keys
/// </summary>
public class GetApiKeysQuery : IRequest<List<ApiKey>>
{
    public bool IncludeInactive { get; set; } = false;
}
