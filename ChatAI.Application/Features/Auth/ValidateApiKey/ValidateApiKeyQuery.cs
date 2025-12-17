using ChatAI.Domain.Entities;
using MediatR;

namespace ChatAI.Application.Features.Auth.ValidateApiKey;

/// <summary>
/// Query to validate an API key and return the key entity if valid
/// </summary>
public class ValidateApiKeyQuery : IRequest<ApiKey?>
{
    public string ApiKey { get; set; } = string.Empty;
}
