using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Client for Facebook Messenger Platform API (Graph API)
/// </summary>
public class MetaMessengerClient : IMetaMessengerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MetaMessengerClient> _logger;
    private readonly string _graphApiVersion;
    private const int MESSAGE_LENGTH_LIMIT = 2000;
    
    public MetaMessengerClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MetaMessengerClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphApiVersion = configuration["Meta:GraphApiVersion"] ?? "v22.0";
    }
    
    public async Task<MetaSendResult> SendMessageAsync(
        string pageAccessToken,
        string recipientPsid,
        string message,
        CancellationToken cancellationToken = default)
    {
        var result = new MetaSendResult();
        
        try
        {
            // Validate and truncate message if needed
            var (processedMessage, wasTruncated, originalLength, truncatedLength) = ProcessMessageLength(message, MESSAGE_LENGTH_LIMIT);
            result.WasTruncated = wasTruncated;
            result.OriginalLength = originalLength;
            result.TruncatedLength = truncatedLength;
            
            if (wasTruncated)
            {
                _logger.LogWarning("Message truncated from {OriginalLength} to {TruncatedLength} chars for Messenger", 
                    originalLength, truncatedLength);
            }
            
            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/{_graphApiVersion}/me/messages?access_token={pageAccessToken}";
            
            var payload = new
            {
                recipient = new { id = recipientPsid },
                message = new { text = processedMessage },
                messaging_type = "RESPONSE"
            };
            
            var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonDocument.Parse(responseContent);
                result.Success = true;
                result.MessageId = jsonResponse.RootElement.GetProperty("message_id").GetString();
                _logger.LogInformation("Messenger message sent successfully: {MessageId}", result.MessageId);
            }
            else
            {
                var jsonResponse = JsonDocument.Parse(responseContent);
                var error = jsonResponse.RootElement.GetProperty("error");
                result.Success = false;
                result.ErrorCode = error.GetProperty("code").GetInt32().ToString();
                result.ErrorMessage = error.GetProperty("message").GetString();
                
                // Check if token error (should disable connection)
                var errorCode = int.Parse(result.ErrorCode);
                result.ShouldDisableConnection = errorCode == 190 || errorCode == 200;
                
                _logger.LogError("Messenger send failed: {ErrorCode} - {ErrorMessage}", 
                    result.ErrorCode, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exception sending Messenger message");
        }
        
        return result;
    }
    
    private (string processedMessage, bool wasTruncated, int originalLength, int truncatedLength) ProcessMessageLength(string message, int limit)
    {
        if (message.Length <= limit)
        {
            return (message, false, message.Length, message.Length);
        }
        
        var truncatedLength = limit - 50; // Reserve space for truncation notice
        var truncated = message.Substring(0, truncatedLength) + "\n\n... (message truncated)";
        
        return (truncated, true, message.Length, truncated.Length);
    }
}
