using ChatAI.Application.Commands;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

/// <summary>
/// Handler for SendChatCommand - orchestrates chat response generation
/// </summary>
public class SendChatCommandHandler : IRequestHandler<SendChatCommand, ChatResponse>
{
    private readonly IChatService _chatService;
    private readonly ILogger<SendChatCommandHandler> _logger;

    public SendChatCommandHandler(
        IChatService chatService,
        ILogger<SendChatCommandHandler> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<ChatResponse> Handle(SendChatCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📨 Handling SendChatCommand for user {UserId}", command.UserId);

        var request = new ChatRequest
        {
            UserId = command.UserId,
            Message = command.Message,
            SessionId = command.SessionId ?? null!,
            UseTools = command.UseTools
        };

        var response = await _chatService.HandleAsync(request);

        _logger.LogInformation("✅ SendChatCommand completed for session {SessionId}", response.SessionId);

        return response;
    }
}
