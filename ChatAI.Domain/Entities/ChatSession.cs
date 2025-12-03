namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a chat conversation session
/// </summary>
public class ChatSession
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? Title { get; set; }
    
    // Optional: Metadata for session management
    public int MessageCount => Messages.Count;
}
