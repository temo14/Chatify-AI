using Azure.AI.OpenAI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Azure OpenAI service connectivity
/// </summary>
public class AzureOpenAIHealthCheck : IHealthCheck
{
    private readonly AzureOpenAIClient _client;
    private readonly ILogger<AzureOpenAIHealthCheck> _logger;
    private readonly string _chatDeployment;

    public AzureOpenAIHealthCheck(
        AzureOpenAIClient client, 
        ILogger<AzureOpenAIHealthCheck> logger,
        string chatDeployment)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatDeployment = chatDeployment ?? throw new ArgumentNullException(nameof(chatDeployment));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple test: get chat client (this validates the deployment exists)
            var chatClient = _client.GetChatClient(_chatDeployment);
            
            if (chatClient == null)
            {
                _logger.LogError("Failed to get chat client for deployment '{Deployment}'", _chatDeployment);
                return HealthCheckResult.Unhealthy(
                    $"Azure OpenAI chat deployment '{_chatDeployment}' is not accessible");
            }

            var data = new Dictionary<string, object>
            {
                { "deployment", _chatDeployment },
                { "status", "accessible" }
            };

            _logger.LogDebug("Azure OpenAI health check passed for deployment '{Deployment}'", _chatDeployment);

            return HealthCheckResult.Healthy(
                $"Azure OpenAI deployment '{_chatDeployment}' is healthy", 
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed for deployment '{Deployment}'", _chatDeployment);
            return HealthCheckResult.Unhealthy(
                $"Azure OpenAI deployment '{_chatDeployment}' is not accessible", 
                ex);
        }
    }
}
