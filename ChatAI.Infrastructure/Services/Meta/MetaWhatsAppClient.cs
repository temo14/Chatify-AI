using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Client for WhatsApp Cloud API (Graph API)
/// </summary>
public class MetaWhatsAppClient : IMetaWhatsAppClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MetaWhatsAppClient> _logger;
    private readonly string _graphApiVersion;
    private const int MESSAGE_LENGTH_LIMIT = 4096;
    
    public MetaWhatsAppClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MetaWhatsAppClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphApiVersion = configuration["Meta:GraphApiVersion"] ?? "v22.0";
    }
    
    public async Task<MetaSendResult> SendMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
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
                _logger.LogWarning("Message truncated from {OriginalLength} to {TruncatedLength} chars for WhatsApp", 
                    originalLength, truncatedLength);
            }
            
            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/messages";
            
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = recipientPhoneNumber,
                type = "text",
                text = new { body = processedMessage }
            };
            
            var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonDocument.Parse(responseContent);
                result.Success = true;
                var messages = jsonResponse.RootElement.GetProperty("messages");
                if (messages.GetArrayLength() > 0)
                {
                    result.MessageId = messages[0].GetProperty("id").GetString();
                }
                _logger.LogInformation("WhatsApp message sent successfully: {MessageId}", result.MessageId);
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
                
                _logger.LogError("WhatsApp send failed: {ErrorCode} - {ErrorMessage}", 
                    result.ErrorCode, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exception sending WhatsApp message");
        }
        
        return result;
    }
    
    public async Task<MetaSendResult> SendTemplateMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
        string templateName,
        string languageCode,
        List<string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = new MetaSendResult();
        
        try
        {
            _logger.LogInformation(
                "Sending WhatsApp template message: Template={TemplateName}, Language={LanguageCode}, To={RecipientPhone}",
                templateName,
                languageCode,
                recipientPhoneNumber);
            
            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/{_graphApiVersion}/{phoneNumberId}/messages";
            
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            // Build template payload
            var templatePayload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = recipientPhoneNumber,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = languageCode },
                    components = parameters != null && parameters.Count > 0
                        ? new[]
                        {
                            new
                            {
                                type = "body",
                                parameters = parameters.Select(p => new { type = "text", text = p }).ToArray()
                            }
                        }
                        : Array.Empty<object>()
                }
            };
            
            var response = await client.PostAsJsonAsync(url, templatePayload, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonDocument.Parse(responseContent);
                result.Success = true;
                var messages = jsonResponse.RootElement.GetProperty("messages");
                if (messages.GetArrayLength() > 0)
                {
                    result.MessageId = messages[0].GetProperty("id").GetString();
                }
                
                _logger.LogInformation(
                    "WhatsApp template message sent successfully: MessageId={MessageId}, Template={TemplateName}",
                    result.MessageId,
                    templateName);
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
                
                _logger.LogError(
                    "WhatsApp template send failed: ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, Template={TemplateName}",
                    result.ErrorCode,
                    result.ErrorMessage,
                    templateName);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exception sending WhatsApp template message: Template={TemplateName}", templateName);
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
