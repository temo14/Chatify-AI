using ChatAI.Domain.Enums;
using MediatR;

namespace ChatAI.Application.Features.MetaChannels.ProcessWebhook;

/// <summary>
/// Command to process an incoming Meta webhook
/// </summary>
public class ProcessMetaWebhookCommand : IRequest<ProcessMetaWebhookResult>
{
    public Guid ConnectionId { get; set; }
    public MetaChannel Channel { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public string EventKey { get; set; } = string.Empty; // For tracing
}

public class ProcessMetaWebhookResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool WasDuplicate { get; set; }
    public bool ReplySent { get; set; }
    public string? MetaMessageId { get; set; }
}
