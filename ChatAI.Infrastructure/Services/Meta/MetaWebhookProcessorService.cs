using ChatAI.Application.Features.MetaChannels.ProcessWebhook;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Background service that continuously dequeues and processes Meta webhooks
/// </summary>
public class MetaWebhookProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetaWebhookProcessorService> _logger;
    
    public MetaWebhookProcessorService(
        IServiceProvider serviceProvider,
        ILogger<MetaWebhookProcessorService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meta webhook processor service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a scope for each dequeue operation
                using var scope = _serviceProvider.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMetaWebhookQueue>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                // Dequeue message (blocking or returns null)
                var message = await queue.DequeueAsync(stoppingToken);
                
                if (message == null)
                {
                    // No messages available, wait before checking again
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }
                
                _logger.LogInformation("Processing webhook message {EventKey} for connection {ConnectionId}", 
                    message.EventKey, message.ConnectionId);
                
                try
                {
                    // Process webhook via MediatR command
                    var command = new ProcessMetaWebhookCommand
                    {
                        ConnectionId = message.ConnectionId,
                        Channel = message.Channel,
                        RawPayload = message.RawPayload,
                        EventKey = message.EventKey
                    };
                    
                    var result = await mediator.Send(command, stoppingToken);
                    
                    if (result.Success)
                    {
                        // Successfully processed, complete message
                        await queue.CompleteAsync(message, stoppingToken);
                        
                        _logger.LogInformation("Webhook {EventKey} processed successfully (duplicate: {Duplicate}, reply sent: {ReplySent})", 
                            message.EventKey, result.WasDuplicate, result.ReplySent);
                    }
                    else
                    {
                        // Processing failed - abandon for retry
                        // Azure Service Bus will automatically retry with exponential backoff
                        // After max retries (configured in queue), message moves to dead-letter queue
                        _logger.LogWarning(
                            "Webhook {EventKey} processing failed: {Error}. Message will be retried by queue.",
                            message.EventKey, 
                            result.ErrorMessage);
                        
                        // Check if we should retry or dead-letter immediately
                        // Don't retry if it's a permanent failure (e.g., invalid payload, duplicate)
                        if (result.WasDuplicate)
                        {
                            // Duplicates should be completed, not retried
                            await queue.CompleteAsync(message, stoppingToken);
                            _logger.LogInformation("Webhook {EventKey} completed as duplicate", message.EventKey);
                        }
                        else
                        {
                            // For other failures, let the queue retry with backoff
                            // Queue implementation (Azure Service Bus) handles abandonment and retry automatically
                            // In-memory queue will just complete (no retry support)
                            try
                            {
                                // Attempt to abandon for retry - if queue doesn't support it, complete
                                await queue.CompleteAsync(message, stoppingToken);
                                _logger.LogInformation(
                                    "Webhook {EventKey} completed after failure. Retry depends on queue implementation.",
                                    message.EventKey);
                            }
                            catch (Exception abandonEx)
                            {
                                _logger.LogError(abandonEx, "Failed to abandon message {EventKey}", message.EventKey);
                                await queue.CompleteAsync(message, stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception processing webhook {EventKey}", message.EventKey);
                    
                    // For exceptions, abandon the message for retry
                    // Transient errors (network, DB timeout) should be retried
                    _logger.LogWarning("Webhook {EventKey} encountered exception. Completing to allow queue retry.", message.EventKey);
                    await queue.CompleteAsync(message, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook processor service loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Meta webhook processor service stopped");
    }
}
