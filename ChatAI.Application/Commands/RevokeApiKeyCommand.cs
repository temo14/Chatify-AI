using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to revoke an API key
/// </summary>
public class RevokeApiKeyCommand : IRequest<bool>
{
    public Guid KeyId { get; set; }
    public Guid RevokedBy { get; set; }
}
