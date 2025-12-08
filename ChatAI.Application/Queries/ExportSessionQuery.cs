using MediatR;

namespace ChatAI.Application.Queries;

/// <summary>
/// Query to export chat session data
/// </summary>
public record ExportSessionQuery : IRequest<ExportSessionResult>
{
    public string SessionId { get; init; } = string.Empty;
    public ExportFormat Format { get; init; } = ExportFormat.Json;
}

/// <summary>
/// Result of session export query
/// </summary>
public class ExportSessionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public SessionExportData? Data { get; set; }
}

/// <summary>
/// Exported session data
/// </summary>
public class SessionExportData
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int MessageCount { get; set; }
    public List<ExportedMessage> Messages { get; set; } = new();
}

/// <summary>
/// Individual exported message
/// </summary>
public class ExportedMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Export format options
/// </summary>
public enum ExportFormat
{
    Json,
    Csv
}
