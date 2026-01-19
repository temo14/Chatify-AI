using Azure.Messaging.ServiceBus;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Azure Service Bus implementation of webhook queue for production durability
/// </summary>
public class AzureServiceBusMetaWebhookQueue : IMetaWebhookQueue, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusMetaWebhookQueue> _logger;
    private readonly string _queueName;

    public AzureServiceBusMetaWebhookQueue(
        IConfiguration configuration,
        ILogger<AzureServiceBusMetaWebhookQueue> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureServiceBus:ConnectionString"] 
            ?? throw new InvalidOperationException("AzureServiceBus:ConnectionString is not configured");
        
        _queueName = configuration["AzureServiceBus:MetaWebhookQueueName"]
            ?? configuration["AzureServiceBus:QueueName"]
            ?? "meta-webhooks";
        
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(_queueName);
        
        _logger.LogInformation("Azure Service Bus webhook queue initialized: {QueueName}", _queueName);
    }

    public async Task EnqueueAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            // If the queue requires sessions, SessionId is mandatory.
            // Primary ordering key: per (connection, external user).
            // Fallback: unique per-message session (still accepted, no ordering guarantees needed).
            var sessionId = string.IsNullOrWhiteSpace(message.ExternalUserId)
                ? $"{message.ConnectionId:D}:{message.EventKey}"
                : $"{message.ConnectionId:D}:{message.ExternalUserId}";

            var serviceBusMessage = new ServiceBusMessage(json)
            {
                MessageId = message.EventKey,
                ContentType = "application/json",
                Subject = message.Channel.ToString(),
                SessionId = sessionId,
                ApplicationProperties =
                {
                    ["ConnectionId"] = message.ConnectionId.ToString(),
                    ["Channel"] = message.Channel.ToString(),
                    ["ExternalUserId"] = message.ExternalUserId ?? string.Empty,
                    ["ReceivedAt"] = message.ReceivedAt.ToString("o")
                }
            };

            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger.LogInformation(
                "Enqueued webhook to Service Bus: ConnectionId={ConnectionId}, Channel={Channel}, EventKey={EventKey}",
                message.ConnectionId, message.Channel, message.EventKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to enqueue webhook to Service Bus: ConnectionId={ConnectionId}, Channel={Channel}",
                message.ConnectionId, message.Channel);
            throw;
        }
    }

    public async Task<MetaWebhookMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Dequeue is handled by the processor service which creates its own receiver
        // This method is not used in the Service Bus implementation
        await Task.CompletedTask;
        throw new NotSupportedException(
            "Dequeue is not supported for Service Bus. Use ServiceBusProcessor in AzureServiceBusMetaWebhookProcessor instead.");
    }

    public async Task CompleteAsync(MetaWebhookMessage message, CancellationToken cancellationToken = default)
    {
        // Complete is handled by the processor service
        // This is a no-op for Service Bus implementation
        await Task.CompletedTask;
    }

    public async Task DeadLetterAsync(MetaWebhookMessage message, string reason, CancellationToken cancellationToken = default)
    {
        // Dead-letter is handled by the processor service
        // This is a no-op for Service Bus implementation
        _logger.LogWarning("DeadLetterAsync called but handled by processor: {Reason}", reason);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
        _logger.LogInformation("Azure Service Bus webhook queue disposed");
    }
}
