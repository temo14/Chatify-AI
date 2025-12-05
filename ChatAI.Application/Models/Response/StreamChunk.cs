namespace ChatAI.Application.Models.Response;

/// <summary>
/// Represents a chunk of streamed chat response
/// </summary>
public class StreamChunk
{
    /// <summary>
    /// Session ID for the chat
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Content chunk (token or group of tokens)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Optional tool call information (appears before content)
    /// </summary>
    public ToolCallInfo? ToolCall { get; set; }

    /// <summary>
    /// Chunk sequence number
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Timestamp of chunk generation
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional error message
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Token usage for this response (only in final chunk)
    /// </summary>
    public int? TotalTokens { get; set; }
}
