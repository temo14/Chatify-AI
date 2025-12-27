using ChatAI.Domain.Enums;

namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a single message in a chat conversation
/// Simple design: Belongs to a session, optional user for future
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Tenant (customer) that owns this message
    /// </summary>
    public Guid TenantId { get; set; }
    
    // Relationships
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; } // Optional - for future authentication
    
    // Message content
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    // Tool call information (Semantic Kernel function execution)
    public bool IsToolCall { get; set; }
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
    public string? ToolResult { get; set; }
    
    // RAG/Embedding support
    public string? EmbeddingReference { get; set; }
    
    // Token usage tracking (for billing)
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    
    // Navigation
    public ChatSession? Session { get; set; }
}
