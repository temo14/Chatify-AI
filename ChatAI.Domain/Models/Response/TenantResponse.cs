namespace ChatAI.Domain.Models.Response;

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PlanTier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CustomDomain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public int MaxDocuments { get; set; }
    public int MaxMonthlyMessages { get; set; }
    public int CurrentDocumentCount { get; set; }
    public int CurrentMonthMessages { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public TenantSettingsResponse? Settings { get; set; }
}

public class TenantSettingsResponse
{
    public Guid Id { get; set; }
    public string VectorStorageMode { get; set; } = string.Empty;
    public string? QdrantCollectionName { get; set; }
    public bool EnableDocumentChunking { get; set; }
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public int ChatHistoryRetentionDays { get; set; }
    public bool EnableFeedback { get; set; }
    public bool EnableOverview { get; set; }
    public bool EnableEmailSupport { get; set; }
    public string? SupportEmail { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? ChatPlaceholder { get; set; }
    public bool EnableTools { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public string? SystemPrompt { get; set; }
}
