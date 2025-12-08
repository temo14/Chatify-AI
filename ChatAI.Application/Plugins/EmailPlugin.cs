using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatAI.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin for sending emails to administrators
/// Allows AI to notify administrators about important issues or requests
/// </summary>
public class EmailPlugin
{
    private readonly IEmailService _emailService;
    private readonly IConfigurationService _configService;
    private readonly ChatContext _chatContext;
    private readonly ILogger<EmailPlugin> _logger;

    public EmailPlugin(
        IEmailService emailService,
        IConfigurationService configService,
        ChatContext chatContext,
        ILogger<EmailPlugin> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _chatContext = chatContext ?? throw new ArgumentNullException(nameof(chatContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send an email notification to the administrator/responsible person
    /// Use this when users request to contact support, report issues, or need human assistance
    /// </summary>
    /// <param name="subject">Email subject (keep it concise and descriptive)</param>
    /// <param name="message">Detailed message explaining the user's request or issue</param>
    /// <returns>Confirmation message</returns>
    [KernelFunction("send_admin_email")]
    [Description("Send an email to the administrator or responsible person. Use this when users want to contact support, report problems, request features, or need human assistance.")]
    public async Task<string> SendAdminEmailAsync(
        [Description("Email subject - brief description of the issue or request")] string subject,
        [Description("Detailed message body explaining what the user needs")] string message)
    {
        try
        {
            _logger.LogInformation("🔧 [{Context}] TOOL CALLED: send_admin_email | Subject: {Subject}", 
                _chatContext.GetContextInfo(), subject);

            if (string.IsNullOrWhiteSpace(subject))
            {
                return "❌ Cannot send email: Subject is required.";
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return "❌ Cannot send email: Message content is required.";
            }

            // Get admin email from configuration (database - Branding.SupportEmail)
            var adminEmail = await _configService.GetValueAsync("Branding.SupportEmail", string.Empty);
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                _logger.LogWarning("Support email not configured in database (Branding.SupportEmail)");
                return "⚠️ Support email is not configured. Please contact your administrator.";
            }

            // Build professional HTML email
            var htmlMessage = BuildHtmlEmail(
                title: "User Support Request",
                subject: subject,
                content: message,
                emailType: "support");

            var success = await _emailService.SendEmailAsync(
                toEmail: adminEmail,
                toName: "Administrator",
                subject: $"[Chatify AI] {subject}",
                message: htmlMessage,
                isHtml: true);

            if (success)
            {
                _logger.LogInformation("✅ [{Context}] TOOL SUCCESS: Email sent to admin", 
                    _chatContext.GetContextInfo());
                return "✅ Your message has been sent to the administrator. They will review it and get back to you soon.";
            }
            else
            {
                _logger.LogWarning("⚠️ [{Context}] TOOL FAILED: Failed to send email to admin", 
                    _chatContext.GetContextInfo());
                return "⚠️ I attempted to send your message, but there was an issue with the email service. Please try contacting support directly.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{Context}] TOOL ERROR: SendAdminEmailAsync failed", 
                _chatContext.GetContextInfo());
            return "❌ An error occurred while trying to send your message. Please try again later or contact support directly.";
        }
    }

    /// <summary>
    /// Send a feature request or suggestion to the administrator
    /// </summary>
    /// <param name="featureDescription">Description of the requested feature or suggestion</param>
    /// <returns>Confirmation message</returns>
    [KernelFunction("send_feature_request")]
    [Description("Send a feature request or suggestion to the administrator. Use when users suggest improvements or new features.")]
    public async Task<string> SendFeatureRequestAsync(
        [Description("Detailed description of the feature request or suggestion")] string featureDescription)
    {
        _logger.LogInformation("🔧 [{Context}] TOOL CALLED: send_feature_request", 
            _chatContext.GetContextInfo());

        if (string.IsNullOrWhiteSpace(featureDescription))
        {
            return "❌ Please provide details about the feature you'd like to suggest.";
        }

        var adminEmail = await _configService.GetValueAsync("Email.AdminEmail", string.Empty);
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return "⚠️ Administrator email is not configured. Please try again later.";
        }

        var htmlMessage = BuildHtmlEmail(
            title: "Feature Request",
            subject: "New Feature Suggestion",
            content: featureDescription,
            emailType: "feature");

        var success = await _emailService.SendEmailAsync(
            toEmail: adminEmail,
            toName: "Administrator",
            subject: "[Chatify AI] Feature Request",
            message: htmlMessage,
            isHtml: true);

        if (success)
        {
            _logger.LogInformation("✅ [{Context}] TOOL SUCCESS: Feature request sent", 
                _chatContext.GetContextInfo());
            return "✅ Thank you for your suggestion! I've forwarded it to the team for review.";
        }
        else
        {
            _logger.LogWarning("⚠️ [{Context}] TOOL FAILED: Failed to send feature request", 
                _chatContext.GetContextInfo());
            return "⚠️ I couldn't submit your feature request at the moment. Please try again later.";
        }
    }

    /// <summary>
    /// Report a bug or technical issue to the administrator
    /// </summary>
    /// <param name="issueDescription">Description of the bug or technical issue</param>
    /// <returns>Confirmation message</returns>
    [KernelFunction("report_bug")]
    [Description("Report a bug or technical issue to the administrator. Use when users report errors, crashes, or unexpected behavior.")]
    public async Task<string> ReportBugAsync(
        [Description("Detailed description of the bug or issue, including steps to reproduce if possible")] string issueDescription)
    {
        _logger.LogInformation("🔧 [{Context}] TOOL CALLED: report_bug", 
            _chatContext.GetContextInfo());

        if (string.IsNullOrWhiteSpace(issueDescription))
        {
            return "❌ Please describe the issue you're experiencing.";
        }

        var adminEmail = await _configService.GetValueAsync("Email.AdminEmail", string.Empty);
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return "⚠️ Administrator email is not configured. Please try again later.";
        }

        var htmlMessage = BuildHtmlEmail(
            title: "Bug Report",
            subject: "Technical Issue Reported",
            content: issueDescription,
            emailType: "bug");

        var success = await _emailService.SendEmailAsync(
            toEmail: adminEmail,
            toName: "Administrator",
            subject: "[Chatify AI] Bug Report",
            message: htmlMessage,
            isHtml: true);

        if (success)
        {
            _logger.LogInformation("✅ [{Context}] TOOL SUCCESS: Bug report sent", 
                _chatContext.GetContextInfo());
            return "✅ Thank you for reporting this issue! The technical team has been notified and will investigate.";
        }
        else
        {
            _logger.LogWarning("⚠️ [{Context}] TOOL FAILED: Failed to send bug report", 
                _chatContext.GetContextInfo());
            return "⚠️ I couldn't submit your bug report at the moment. Please try contacting support directly.";
        }
    }

    /// <summary>
    /// Build a professional HTML email template
    /// </summary>
    private string BuildHtmlEmail(string title, string subject, string content, string emailType)
    {
        var (iconColor, iconEmoji) = emailType switch
        {
            "support" => ("#3b82f6", "💬"),
            "feature" => ("#10b981", "💡"),
            "bug" => ("#ef4444", "🐛"),
            _ => ("#667eea", "📧")
        };

        var contentLabel = emailType switch
        {
            "bug" => "Issue Description",
            "feature" => "Feature Description",
            _ => "Message"
        };

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f7f9fc;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f7f9fc; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px 40px; text-align: center;"">
                            <h1 style=""margin: 0; color: white; font-size: 24px; font-weight: 600;"">{iconEmoji} {title}</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <div style=""background-color: #f8fafc; border-left: 4px solid {iconColor}; padding: 20px; margin-bottom: 24px; border-radius: 4px;"">
                                <h2 style=""margin: 0 0 8px 0; font-size: 16px; color: #334155; font-weight: 600;"">{subject}</h2>
                                <p style=""margin: 0; color: #64748b; font-size: 13px;"">Automatically generated by Chatify AI Assistant</p>
                            </div>
                            
                            <div style=""background-color: white; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 24px;"">
                                <h3 style=""margin: 0 0 16px 0; font-size: 14px; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px;"">
                                    {contentLabel}
                                </h3>
                                <div style=""color: #334155; font-size: 15px; line-height: 1.6; white-space: pre-wrap;"">
{content}
                                </div>
                            </div>
                            
                            <div style=""background-color: #fef3c7; border: 1px solid #fbbf24; padding: 16px; border-radius: 6px; margin-bottom: 24px;"">
                                <p style=""margin: 0; color: #92400e; font-size: 13px;"">
                                    <strong>⚠️ Action Required:</strong> This {emailType} request requires your review and response.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8fafc; padding: 24px 40px; border-top: 1px solid #e2e8f0; text-align: center;"">
                            <p style=""margin: 0 0 8px 0; color: #64748b; font-size: 12px;"">
                                <strong>Chatify AI Assistant</strong>
                            </p>
                            <p style=""margin: 0; color: #94a3b8; font-size: 11px;"">
                                Sent: {DateTime.UtcNow:dddd, MMMM dd, yyyy 'at' HH:mm:ss} UTC
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
";
    }
}
