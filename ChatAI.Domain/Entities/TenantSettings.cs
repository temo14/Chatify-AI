// ChatAI.Domain/Entities/TenantSettings.cs
namespace ChatAI.Domain.Entities;

/// <summary>
/// Tenant-specific configuration and feature flags
/// </summary>
public class TenantSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>
    /// Vector storage mode: "SQL" or "Qdrant"
    /// SQL: embeddings stored in database (< 100 docs, cheapest)
    /// Qdrant: use Qdrant vector DB (> 100 docs, best performance)
    /// </summary>
    public string VectorStorageMode { get; set; } = "SQL"; // SQL | Qdrant

    /// <summary>
    /// Qdrant collection name (if using Qdrant mode)
    /// </summary>
    public string? QdrantCollectionName { get; set; }

    /// <summary>
    /// Enable document chunking for large documents
    /// </summary>
    public bool EnableDocumentChunking { get; set; } = true;

    /// <summary>
    /// Chunk size in tokens (default 512)
    /// </summary>
    public int ChunkSize { get; set; } = 512;

    /// <summary>
    /// Chunk overlap in tokens (default 50)
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;

    /// <summary>
    /// Days to retain chat history (0 = no history, 1 = today only, 7/30/90 = retention period)
    /// Messages older than this will not be loaded into conversation context
    /// </summary>
    public int ChatHistoryRetentionDays { get; set; } = 90;

    /// <summary>
    /// Enable feedback collection and feedback tab in admin UI
    /// </summary>
    public bool EnableFeedback { get; set; } = true;

    /// <summary>
    /// Enable overview/analytics dashboard tab in admin UI
    /// </summary>
    public bool EnableOverview { get; set; } = true;

    /// <summary>
    /// Enable email support feature (allows tenant to configure support email and send support messages)
    /// </summary>
    public bool EnableEmailSupport { get; set; } = false;

    /// <summary>
    /// Support email address where messages will be sent (if email support is enabled)
    /// </summary>
    /// <summary>
    /// Customer-facing support email where end-users can send inquiries
    /// This is displayed to end-users in the chat interface
    /// The AI will send support requests from customers to this email
    /// Example: support@musicstudio.com or help@clinic.com
    /// Different from Tenant.Email (business contact) and AdminUser.Email (individual admin)
    /// </summary>
    public string? SupportEmail { get; set; }

    /// <summary>
    /// Welcome message for chat widget
    /// </summary>
    public string? WelcomeMessage { get; set; }

    /// <summary>
    /// Placeholder text for chat input
    /// </summary>
    public string? ChatPlaceholder { get; set; } = "Ask me anything...";

    /// <summary>
    /// Enable AI tools/function calling
    /// </summary>
    public bool EnableTools { get; set; } = true;

    /// <summary>
    /// AI temperature (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Max tokens per response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// System prompt override (null = use default)
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Navigation
    /// </summary>
    public Tenant? Tenant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}