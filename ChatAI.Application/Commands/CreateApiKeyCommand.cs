using ChatAI.Application.DTOs;
using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to create a new API key
/// </summary>
public class CreateApiKeyCommand : IRequest<ApiKeyResponseDto>
{
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RateLimitPerMinute { get; set; } = 20;
    public int RateLimitPerDay { get; set; } = 1000;
    public DateTime? ExpiresAt { get; set; }
    public Guid CreatedBy { get; set; }
}
