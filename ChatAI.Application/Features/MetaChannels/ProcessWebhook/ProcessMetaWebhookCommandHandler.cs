using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Request;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Application.Features.MetaChannels.ProcessWebhook;

/// <summary>
/// Handler for processing Meta webhooks
/// This is the core integration logic that receives messages, generates replies, and sends them back
/// </summary>
public class ProcessMetaWebhookCommandHandler : IRequestHandler<ProcessMetaWebhookCommand, ProcessMetaWebhookResult>
{
    private static readonly HashSet<string> StopKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "STOP",
        "STOPALL",
        "STOP ALL",
        "UNSUBSCRIBE",
        "CANCEL",
        "END",
        "QUIT"
    };

    private static readonly HashSet<string> StartKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "START",
        "STARTALL",
        "START ALL"
    };

    private readonly IMetaChannelConnectionRepository _connectionRepository;
    private readonly IMetaInboundDedupeRepository _dedupeRepository;
    private readonly IMetaConversationMapRepository _conversationMapRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IChatService _chatService;
    private readonly IMetaMessengerClient _messengerClient;
    private readonly IMetaInstagramClient _instagramClient;
    private readonly IMetaWhatsAppClient _whatsAppClient;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProcessMetaWebhookCommandHandler> _logger;
    
    public ProcessMetaWebhookCommandHandler(
        IMetaChannelConnectionRepository connectionRepository,
        IMetaInboundDedupeRepository dedupeRepository,
        IMetaConversationMapRepository conversationMapRepository,
        IEncryptionService encryptionService,
        IChatService chatService,
        IMetaMessengerClient messengerClient,
        IMetaInstagramClient instagramClient,
        IMetaWhatsAppClient whatsAppClient,
        ITenantContext tenantContext,
        ILogger<ProcessMetaWebhookCommandHandler> logger)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _dedupeRepository = dedupeRepository ?? throw new ArgumentNullException(nameof(dedupeRepository));
        _conversationMapRepository = conversationMapRepository ?? throw new ArgumentNullException(nameof(conversationMapRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _messengerClient = messengerClient ?? throw new ArgumentNullException(nameof(messengerClient));
        _instagramClient = instagramClient ?? throw new ArgumentNullException(nameof(instagramClient));
        _whatsAppClient = whatsAppClient ?? throw new ArgumentNullException(nameof(whatsAppClient));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ProcessMetaWebhookResult> Handle(ProcessMetaWebhookCommand command, CancellationToken cancellationToken)
    {
        var result = new ProcessMetaWebhookResult();
        
        try
        {
            _logger.LogInformation("Processing Meta webhook {EventKey} for connection {ConnectionId}", 
                command.EventKey, command.ConnectionId);
            
            // 1. Get connection (bypasses tenant filter) - webhooks don't have tenant context yet
            var connection = await _connectionRepository.GetByIdWithoutTenantFilterAsync(command.ConnectionId, cancellationToken);
            if (connection == null)
            {
                _logger.LogError("Connection not found: {ConnectionId}", command.ConnectionId);
                result.Success = false;
                result.ErrorMessage = "Connection not found";
                return result;
            }
            
            if (!connection.IsActive)
            {
                _logger.LogWarning("Connection {ConnectionId} is inactive, skipping", command.ConnectionId);
                result.Success = false;
                result.ErrorMessage = "Connection is inactive";
                return result;
            }
            
            // CRITICAL: Set tenant context from connection for downstream services
            // This is necessary because webhook requests don't have JWT/domain resolution
            _tenantContext.SetTenant(connection.TenantId, connection.TenantId.ToString("N"));
            
            _logger.LogInformation("Tenant context set to {TenantId} for webhook processing", connection.TenantId);
            
            // 2. Parse webhook payload
            var inboundMessage = ParseWebhookPayload(command.Channel, command.RawPayload);
            if (inboundMessage == null)
            {
                _logger.LogDebug("Skipping non-message webhook event for connection {ConnectionId} (likely status update or test event)", command.ConnectionId);
                result.Success = true; // Don't retry parsing errors
                return result;
            }
            
            result.MetaMessageId = inboundMessage.MessageId;
            
            // 3. Deduplicate
            var isNew = await _dedupeRepository.RecordMessageAsync(
                connection.Id, 
                inboundMessage.MessageId, 
                cancellationToken);
            
            if (!isNew)
            {
                _logger.LogDebug("Duplicate message {MessageId} for connection {ConnectionId}", 
                    inboundMessage.MessageId, command.ConnectionId);
                result.Success = true;
                result.WasDuplicate = true;
                return result;
            }
            
            // 4. Resolve or create conversation mapping
            var conversationMap = await _conversationMapRepository.GetByExternalUserIdAsync(
                connection.Id, 
                inboundMessage.SenderId, 
                cancellationToken);
            
            string sessionId;
            if (conversationMap == null)
            {
                // Create new mapping with pattern: {channel}:{connectionId}:{externalUserId}
                sessionId = $"{command.Channel}:{connection.Id:N}:{inboundMessage.SenderId}";
                conversationMap = new MetaConversationMap
                {
                    ConnectionId = connection.Id,
                    ExternalUserId = inboundMessage.SenderId,
                    ChatSessionId = sessionId
                };
                await _conversationMapRepository.CreateAsync(conversationMap, cancellationToken);
                _logger.LogInformation("Created new conversation map: {ExternalUserId} -> {SessionId}", 
                    inboundMessage.SenderId, sessionId);
            }
            else
            {
                sessionId = conversationMap.ChatSessionId;
                await _conversationMapRepository.UpdateLastActivityAsync(conversationMap.Id, cancellationToken);
            }

            // 4.1 STOP/START opt-out handling (applies to all channels; required for WhatsApp)
            var normalizedText = NormalizeCommandText(inboundMessage.Text);
            if (IsStopCommand(normalizedText))
            {
                await _conversationMapRepository.SetOptOutAsync(conversationMap.Id, isOptedOut: true, cancellationToken);
                _logger.LogInformation("User opted out (STOP). Connection={ConnectionId} ExternalUser={ExternalUserId}",
                    connection.Id, inboundMessage.SenderId);
                result.Success = true;
                result.ReplySent = false;
                return result;
            }

            if (IsStartCommand(normalizedText))
            {
                await _conversationMapRepository.SetOptOutAsync(conversationMap.Id, isOptedOut: false, cancellationToken);
                _logger.LogInformation("User opted in (START). Connection={ConnectionId} ExternalUser={ExternalUserId}",
                    connection.Id, inboundMessage.SenderId);
                result.Success = true;
                result.ReplySent = false;
                return result;
            }

            if (conversationMap.IsOptedOut)
            {
                _logger.LogInformation("Skipping reply because user is opted out. Connection={ConnectionId} ExternalUser={ExternalUserId}",
                    connection.Id, inboundMessage.SenderId);
                result.Success = true;
                result.ReplySent = false;
                return result;
            }

            // 4.2 24-hour messaging window: if processing is delayed > 24h, do not send
            if (inboundMessage.SentAtUtc.HasValue)
            {
                var age = DateTime.UtcNow - inboundMessage.SentAtUtc.Value;
                if (age > TimeSpan.FromHours(24))
                {
                    _logger.LogWarning("Skipping reply due to 24h window. Age={AgeHours:F1}h Connection={ConnectionId} ExternalUser={ExternalUserId}",
                        age.TotalHours, connection.Id, inboundMessage.SenderId);
                    result.Success = true;
                    result.ReplySent = false;
                    return result;
                }
            }
            
            // 5. Generate AI reply using existing chat service
            var chatRequest = new ChatRequest
            {
                UserId = inboundMessage.SenderId,
                Message = inboundMessage.Text,
                SessionId = sessionId,
                UseTools = true // Enable tools for Meta channels
            };
            
            var chatResponse = await _chatService.HandleAsync(chatRequest);
            
            _logger.LogInformation("Generated AI reply for session {SessionId}: {ReplyLength} chars", 
                sessionId, chatResponse.Reply.Length);
            
            // 6. Send reply via appropriate channel
            var decryptedToken = _encryptionService.Decrypt(connection.AccessTokenEncrypted, connection.TokenKeyVersion);
            
            var sendResult = command.Channel switch
            {
                MetaChannel.Messenger => await _messengerClient.SendMessageAsync(
                    decryptedToken,
                    inboundMessage.SenderId,
                    chatResponse.Reply,
                    cancellationToken),
                
                MetaChannel.Instagram => await _instagramClient.SendMessageAsync(
                    decryptedToken,
                    inboundMessage.SenderId,
                    chatResponse.Reply,
                    cancellationToken),
                
                MetaChannel.WhatsApp => await _whatsAppClient.SendMessageAsync(
                    decryptedToken,
                    connection.WhatsAppPhoneNumberId!,
                    inboundMessage.SenderId,
                    chatResponse.Reply,
                    cancellationToken),
                
                _ => throw new NotSupportedException($"Channel {command.Channel} not supported")
            };
            
            // 7. Update connection statistics
            connection.LastWebhookAt = DateTime.UtcNow;
            
            if (sendResult.Success)
            {
                connection.LastSendAt = DateTime.UtcNow;
                connection.FailedSendCount = 0; // Reset on success
                result.Success = true;
                result.ReplySent = true;
                
                _logger.LogInformation("Reply sent successfully via {Channel}: {MessageId}", 
                    command.Channel, sendResult.MessageId);
                
                if (sendResult.WasTruncated)
                {
                    _logger.LogWarning("Reply was truncated: {Original} -> {Truncated} chars", 
                        sendResult.OriginalLength, sendResult.TruncatedLength);
                }
            }
            else
            {
                connection.FailedSendCount++;
                connection.LastError = $"[{sendResult.ErrorCode}] {sendResult.ErrorMessage}";
                connection.LastErrorAt = DateTime.UtcNow;
                
                // Auto-disable connection after threshold
                if (connection.FailedSendCount >= 10 || sendResult.ShouldDisableConnection)
                {
                    connection.IsActive = false;
                    _logger.LogError("Connection {ConnectionId} auto-disabled after {FailedCount} failures or token error", 
                        connection.Id, connection.FailedSendCount);
                }
                
                result.Success = false;
                result.ErrorMessage = sendResult.ErrorMessage;
                
                _logger.LogError("Failed to send reply via {Channel}: {Error}", 
                    command.Channel, sendResult.ErrorMessage);
            }
            
            await _connectionRepository.UpdateAsync(connection, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook {EventKey}", command.EventKey);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private InboundMessage? ParseWebhookPayload(MetaChannel channel, string rawPayload)
    {
        try
        {
            var json = JsonDocument.Parse(rawPayload);
            var root = json.RootElement;
            
            // Meta webhooks have structure: { "entry": [ { "messaging": [...] } ] } or { "entry": [ { "changes": [...] } ] }
            if (!root.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
            {
                return null;
            }
            
            var entry = entries[0];
            
            return channel switch
            {
                MetaChannel.Messenger => ParseMessengerWebhook(entry),
                MetaChannel.Instagram => ParseInstagramWebhook(entry),
                MetaChannel.WhatsApp => ParseWhatsAppWebhook(entry),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing webhook payload");
            return null;
        }
    }
    
    private InboundMessage? ParseMessengerWebhook(JsonElement entry)
    {
        if (!entry.TryGetProperty("messaging", out var messagingArray) || messagingArray.GetArrayLength() == 0)
        {
            return null;
        }
        
        var messaging = messagingArray[0];
        
        // Skip if not a message event
        if (!messaging.TryGetProperty("message", out var message))
        {
            return null;
        }
        
        // Skip echo events (our own sent messages)
        if (message.TryGetProperty("is_echo", out var isEcho) && isEcho.GetBoolean())
        {
            return null;
        }
        
        var sender = messaging.GetProperty("sender").GetProperty("id").GetString()!;
        var mid = message.GetProperty("mid").GetString()!;
        var text = message.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        DateTime? sentAtUtc = null;
        if (messaging.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var ms))
        {
            sentAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }
        
        if (string.IsNullOrEmpty(text))
        {
            return null; // Skip non-text messages
        }
        
        return new InboundMessage
        {
            MessageId = mid,
            SenderId = sender,
            Text = text,
            SentAtUtc = sentAtUtc
        };
    }
    
    private InboundMessage? ParseInstagramWebhook(JsonElement entry)
    {
        if (!entry.TryGetProperty("messaging", out var messagingArray) || messagingArray.GetArrayLength() == 0)
        {
            return null;
        }
        
        var messaging = messagingArray[0];
        
        // Skip if not a message event
        if (!messaging.TryGetProperty("message", out var message))
        {
            return null;
        }
        
        var sender = messaging.GetProperty("sender").GetProperty("id").GetString()!;
        var mid = message.GetProperty("mid").GetString()!;
        var text = message.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        DateTime? sentAtUtc = null;
        if (messaging.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var ms))
        {
            sentAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }
        
        if (string.IsNullOrEmpty(text))
        {
            return null; // Skip non-text messages
        }
        
        return new InboundMessage
        {
            MessageId = mid,
            SenderId = sender,
            Text = text,
            SentAtUtc = sentAtUtc
        };
    }
    
    private InboundMessage? ParseWhatsAppWebhook(JsonElement entry)
    {
        if (!entry.TryGetProperty("changes", out var changesArray) || changesArray.GetArrayLength() == 0)
        {
            return null;
        }
        
        var change = changesArray[0];
        if (!change.TryGetProperty("value", out var value))
        {
            return null;
        }
        
        if (!value.TryGetProperty("messages", out var messagesArray) || messagesArray.GetArrayLength() == 0)
        {
            return null; // Could be a status update, not a message
        }
        
        var message = messagesArray[0];
        
        var messageId = message.GetProperty("id").GetString()!;
        var from = message.GetProperty("from").GetString()!;
        DateTime? sentAtUtc = null;
        if (message.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.String)
        {
            if (long.TryParse(ts.GetString(), out var seconds))
            {
                sentAtUtc = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
        }
        
        // Only process text messages
        if (!message.TryGetProperty("text", out var textObj))
        {
            return null;
        }
        
        var text = textObj.GetProperty("body").GetString();
        
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        
        return new InboundMessage
        {
            MessageId = messageId,
            SenderId = from,
            Text = text,
            SentAtUtc = sentAtUtc
        };
    }
    
    private class InboundMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime? SentAtUtc { get; set; }
    }

    private static string NormalizeCommandText(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        // Collapse whitespace
        var parts = trimmed.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    private static bool IsStopCommand(string normalizedText)
        => StopKeywords.Contains(normalizedText) || StopKeywords.Contains(normalizedText.Replace(" ", string.Empty));

    private static bool IsStartCommand(string normalizedText)
        => StartKeywords.Contains(normalizedText) || StartKeywords.Contains(normalizedText.Replace(" ", string.Empty));
}
