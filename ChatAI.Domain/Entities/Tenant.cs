// ChatAI.Domain/Entities/Tenant.cs
namespace ChatAI.Domain.Entities;

/// <summary>
/// Represents a tenant (customer) in the multi-tenant system
/// Each tenant is typically a small business (e.g., music studio, clinic, shop)
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique tenant identifier (used in subdomain: {slug}.yourapp.com)
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Business/organization name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Subscription plan tier
    /// </summary>
    public string PlanTier { get; set; } = "Free"; // Free, Starter, Pro, Enterprise

    /// <summary>
    /// Whether the tenant account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Custom domain (optional): chat.musicstudio.com
    /// </summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// Logo URL for branding
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex)
    /// </summary>
    public string? PrimaryColor { get; set; } = "#667eea";

    /// <summary>
    /// Maximum documents allowed (based on plan)
    /// </summary>
    public int MaxDocuments { get; set; } = 10;

    /// <summary>
    /// Maximum monthly messages (based on plan)
    /// </summary>
    public int MaxMonthlyMessages { get; set; } = 1000;

    /// <summary>
    /// Current document count (cached for quick checks)
    /// </summary>
    public int CurrentDocumentCount { get; set; } = 0;

    /// <summary>
    /// Messages used this billing period
    /// </summary>
    public int CurrentMonthMessages { get; set; } = 0;

    /// <summary>
    /// When billing period resets
    /// </summary>
    public DateTime BillingPeriodStart { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant settings as JSON
    /// </summary>
    public string? SettingsJson { get; set; }

    /// <summary>
    /// When tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Subscription expiry date (null = active)
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Navigation: Tenant-specific settings
    /// </summary>
    public TenantSettings? Settings { get; set; }
}