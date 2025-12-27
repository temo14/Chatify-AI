using MediatR;

namespace ChatAI.Application.Features.Auth.CreateApiKey;

/// <summary>
/// Command to create a new API key
/// </summary>
public class CreateApiKeyCommand : IRequest<ApiKeyResult>
{
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RateLimitPerMinute { get; set; } = 20;
    public int RateLimitPerDay { get; set; } = 1000;
    public DateTime? ExpiresAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid TenantId { get; set; } // Tenant context for the API key
}
