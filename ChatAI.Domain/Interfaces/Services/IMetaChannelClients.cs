using ChatAI.Domain.Models;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Client for sending messages via Facebook Messenger (Graph API)
/// </summary>
public interface IMetaMessengerClient
{
    /// <summary>
    /// Send a text message via Messenger
    /// </summary>
    /// <param name="pageAccessToken">Page access token</param>
    /// <param name="recipientPsid">Recipient's Page-Scoped ID</param>
    /// <param name="message">Message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MetaSendResult> SendMessageAsync(
        string pageAccessToken,
        string recipientPsid,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for sending messages via Instagram Messaging API
/// </summary>
public interface IMetaInstagramClient
{
    /// <summary>
    /// Send a text message via Instagram
    /// </summary>
    /// <param name="accessToken">Instagram account access token</param>
    /// <param name="recipientId">Recipient's Instagram user ID</param>
    /// <param name="message">Message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MetaSendResult> SendMessageAsync(
        string accessToken,
        string recipientId,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Client for sending messages via WhatsApp Cloud API
/// </summary>
public interface IMetaWhatsAppClient
{
    /// <summary>
    /// Send a text message via WhatsApp
    /// </summary>
    /// <param name="accessToken">WhatsApp Cloud API token</param>
    /// <param name="phoneNumberId">WhatsApp phone number ID</param>
    /// <param name="recipientPhoneNumber">Recipient's phone number (wa_id)</param>
    /// <param name="message">Message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MetaSendResult> SendMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
        string message,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a template message via WhatsApp (for messages outside 24-hour window)
    /// </summary>
    /// <param name="accessToken">WhatsApp Cloud API token</param>
    /// <param name="phoneNumberId">WhatsApp phone number ID</param>
    /// <param name="recipientPhoneNumber">Recipient's phone number (wa_id)</param>
    /// <param name="templateName">Approved template name</param>
    /// <param name="languageCode">Template language code (e.g., "en_US")</param>
    /// <param name="parameters">Template parameter values (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MetaSendResult> SendTemplateMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
        string templateName,
        string languageCode,
        List<string>? parameters = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for validating Meta access tokens
/// </summary>
public interface IMetaTokenValidator
{
    /// <summary>
    /// Validate a Meta access token using debug_token endpoint
    /// </summary>
    Task<MetaTokenValidationResult> ValidateTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
