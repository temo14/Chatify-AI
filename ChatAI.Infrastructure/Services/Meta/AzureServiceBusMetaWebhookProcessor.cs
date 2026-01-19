using Azure.Messaging.ServiceBus;
using ChatAI.Application.Features.MetaChannels.ProcessWebhook;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Background service for processing Meta webhooks from Azure Service Bus
/// </summary>
public class AzureServiceBusMetaWebhookProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureServiceBusMetaWebhookProcessor> _logger;
    private ServiceBusSessionProcessor? _processor;
    private ServiceBusClient? _client;

    public AzureServiceBusMetaWebhookProcessor(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AzureServiceBusMetaWebhookProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Azure Service Bus Meta webhook processor starting");

        try
        {
            var connectionString = _configuration["AzureServiceBus:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("AzureServiceBus:ConnectionString not configured. Service Bus processor will not start.");
                return;
            }

            var queueName = _configuration["AzureServiceBus:MetaWebhookQueueName"]
                ?? _configuration["AzureServiceBus:QueueName"]
                ?? "meta-webhooks";

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateSessionProcessor(queueName, new ServiceBusSessionProcessorOptions
            {
                MaxConcurrentSessions = 50,
                MaxConcurrentCallsPerSession = 1,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
                PrefetchCount = 5
            });

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Azure Service Bus processor started for queue: {QueueName}", queueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Azure Service Bus processor stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Azure Service Bus processor");
            throw;
        }
    }

    private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        
        try
        {
            _logger.LogInformation("Processing Service Bus message {MessageId}", messageId);

            // Deserialize queue item
            var json = args.Message.Body.ToString();
            var message = JsonSerializer.Deserialize<MetaWebhookMessage>(json);

            if (message == null)
            {
                _logger.LogError("Failed to deserialize queue item from message {MessageId}", messageId);
                await args.DeadLetterMessageAsync(args.Message, 
                    "Deserialization failed", 
                    "Could not deserialize MetaWebhookMessage");
                return;
            }

            // Process webhook via MediatR command
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new ProcessMetaWebhookCommand
            {
                ConnectionId = message.ConnectionId,
                Channel = message.Channel,
                RawPayload = message.RawPayload,
                EventKey = message.EventKey
            };

            var result = await mediator.Send(command, args.CancellationToken);

            if (result.Success)
            {
                // Successfully processed
                await args.CompleteMessageAsync(args.Message);
                
                _logger.LogInformation(
                    "Webhook {EventKey} processed successfully (duplicate: {Duplicate}, reply sent: {ReplySent})",
                    message.EventKey, result.WasDuplicate, result.ReplySent);
            }
            else
            {
                // Processing failed
                _logger.LogError("Webhook {EventKey} processing failed: {Error}", 
                    message.EventKey, result.ErrorMessage);

                // Check delivery count for retry logic
                if (args.Message.DeliveryCount >= 4)
                {
                    // Max retries exceeded, dead-letter
                    await args.DeadLetterMessageAsync(args.Message,
                        "MaxRetryExceeded",
                        $"Processing failed after {args.Message.DeliveryCount} attempts: {result.ErrorMessage}");
                    
                    _logger.LogWarning("Message {MessageId} dead-lettered after {DeliveryCount} attempts",
                        messageId, args.Message.DeliveryCount);
                }
                else
                {
                    // Abandon for retry (exponential backoff handled by Service Bus)
                    await args.AbandonMessageAsync(args.Message);
                    
                    _logger.LogInformation("Message {MessageId} abandoned for retry (attempt {DeliveryCount})",
                        messageId, args.Message.DeliveryCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception processing Service Bus message {MessageId}", messageId);

            try
            {
                if (args.Message.DeliveryCount >= 4)
                {
                    await args.DeadLetterMessageAsync(args.Message,
                        "ProcessingException",
                        ex.Message);
                }
                else
                {
                    await args.AbandonMessageAsync(args.Message);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to handle message error for {MessageId}", messageId);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus error: Source={ErrorSource}, Entity={EntityPath}, Namespace={Namespace}",
            args.ErrorSource, args.EntityPath, args.FullyQualifiedNamespace);
        
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping Azure Service Bus processor");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}
