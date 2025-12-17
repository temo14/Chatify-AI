using ChatAI.Application.Configuration;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> SendToAdminAsync(
        string subject,
        string message,
        bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            _logger.LogWarning("Admin email not configured. Cannot send email with subject: {Subject}", subject);
            return Task.FromResult(false);
        }

        return SendEmailAsync(
            _options.AdminEmail,
            _options.AdminName,
            subject,
            message,
            isHtml,
            cancellationToken);
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string message,
        bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        // Validate email is enabled
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email is disabled. Skipping email to {Email} with subject: {Subject}", 
                toEmail, subject);
            return false;
        }

        // Validate configuration
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || 
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError("Email service not properly configured. Missing SMTP host or from email.");
            return false;
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = message,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(new MailAddress(toEmail, toName));

            using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = _options.EnableSsl,
                Timeout = _options.TimeoutSeconds * 1000
            };

            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", toEmail, subject);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", toEmail);
            return false;
        }
    }
}
