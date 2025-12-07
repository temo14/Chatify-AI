namespace ChatAI.Application.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email to administrator
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email body (plain text or HTML)</param>
    /// <param name="isHtml">Whether the message is HTML formatted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendToAdminAsync(
        string subject, 
        string message, 
        bool isHtml = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email to a specific recipient
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="subject">Email subject</param>
    /// <param name="message">Email body (plain text or HTML)</param>
    /// <param name="isHtml">Whether the message is HTML formatted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string message,
        bool isHtml = false,
        CancellationToken cancellationToken = default);
}
