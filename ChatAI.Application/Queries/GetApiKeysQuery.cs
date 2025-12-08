using ChatAI.Application.DTOs;
using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to get all API keys
/// </summary>
public class GetApiKeysQuery : IRequest<List<ApiKeyResponseDto>>
{
    public bool IncludeInactive { get; set; } = false;
}
