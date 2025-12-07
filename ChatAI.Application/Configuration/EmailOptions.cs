namespace ChatAI.Application.Configuration;

/// <summary>
/// Configuration options for email functionality
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server hostname (e.g., smtp.gmail.com, smtp-mail.outlook.com)
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (587 for TLS, 465 for SSL, 25 for non-encrypted)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Email address to send from
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Display name for sender
    /// </summary>
    public string FromName { get; set; } = "Chatify AI";

    /// <summary>
    /// SMTP username (usually same as FromEmail)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password or app-specific password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS encryption
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Administrator email address to receive notifications
    /// </summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Administrator name
    /// </summary>
    public string AdminName { get; set; } = "Administrator";

    /// <summary>
    /// Timeout in seconds for SMTP operations
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable email functionality (allows disabling in development)
    /// </summary>
    public bool Enabled { get; set; } = true;
}
