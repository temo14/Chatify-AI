using ChatAI.Domain.Entities;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Data;

/// <summary>
/// Database context for Chatify AI
/// Multi-tenant system with global query filters
/// </summary>
public class ChatDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public ChatDbContext(
        DbContextOptions<ChatDbContext> options,
        ITenantContext? tenantContext = null) : base(options)
    {
        _tenantContext = tenantContext;
    }
    
    // Multi-tenancy
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantSettings> TenantSettings { get; set; } = null!;
    
    // Chat data
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    
    // Knowledge base (RAG) - managed via future control panel
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = null!;
    
    // Feedback and configuration
    public DbSet<MessageFeedback> MessageFeedbacks { get; set; } = null!;
    public DbSet<AdminConfiguration> AdminConfigurations { get; set; } = null!;
    
    // Authentication
    public DbSet<AdminUser> AdminUsers { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== CHAT SESSION =====
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("ChatSessions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(100); // Nullable
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.SessionMetadata).HasColumnType("nvarchar(max)"); // JSON
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired();
            
            // Indexes for queries
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ChatSessions_TenantId");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ChatSessions_CreatedAt");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatSessions_UserId");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ChatSessions_IsActive");
            entity.HasIndex(e => e.LastActivityAt).HasDatabaseName("IX_ChatSessions_LastActivity");
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt }).HasDatabaseName("IX_ChatSessions_Tenant_Created");
            
            // Ignore computed properties
            entity.Ignore(e => e.Messages);
            entity.Ignore(e => e.MessageCount);
            entity.Ignore(e => e.IsAnonymous);
        });

        // ===== CHAT MESSAGE =====
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(100); // Nullable
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
            
            // Tool call fields (Semantic Kernel plugins)
            entity.Property(e => e.IsToolCall).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ToolName).HasMaxLength(100);
            entity.Property(e => e.ToolArguments).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ToolResult).HasColumnType("nvarchar(max)");
            
            // Token tracking (for cost analysis)
            entity.Property(e => e.InputTokens);
            entity.Property(e => e.OutputTokens);
            entity.Property(e => e.TotalTokens);
            
            entity.Property(e => e.EmbeddingReference).HasMaxLength(200);
            
            // Relationships
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade); // Delete messages when session deleted
            
            // Query indexes
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ChatMessages_TenantId");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("IX_ChatMessages_SessionId");
            entity.HasIndex(e => new { e.SessionId, e.Timestamp }).HasDatabaseName("IX_ChatMessages_Session_Time");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_ChatMessages_Timestamp");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatMessages_UserId");
            entity.HasIndex(e => new { e.TenantId, e.Timestamp }).HasDatabaseName("IX_ChatMessages_Tenant_Time");
        });

        // ===== KNOWLEDGE DOCUMENT (RAG) =====
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.ToTable("KnowledgeDocuments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.EmbeddingReference).HasMaxLength(200);
            entity.Property(e => e.EmbeddingData).HasColumnType("nvarchar(max)"); // Full embedding JSON for SQL mode
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)"); // JSON
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            
            // Query indexes (for control panel and RAG search)
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_KnowledgeDocuments_TenantId");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_KnowledgeDocuments_Category");
            entity.HasIndex(e => e.Source).HasDatabaseName("IX_KnowledgeDocuments_Source");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_KnowledgeDocuments_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_KnowledgeDocuments_CreatedAt");
            entity.HasIndex(e => new { e.Category, e.IsActive }).HasDatabaseName("IX_KnowledgeDocuments_Category_Active");
            entity.HasIndex(e => new { e.TenantId, e.IsActive }).HasDatabaseName("IX_KnowledgeDocuments_Tenant_Active");
        });
        
        // ===== MESSAGE FEEDBACK =====
        modelBuilder.Entity<MessageFeedback>(entity =>
        {
            entity.ToTable("MessageFeedbacks");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes for analytics queries
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_MessageFeedbacks_TenantId");
            entity.HasIndex(e => e.MessageId).HasDatabaseName("IX_MessageFeedbacks_MessageId");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("IX_MessageFeedbacks_SessionId");
            entity.HasIndex(e => e.Rating).HasDatabaseName("IX_MessageFeedbacks_Rating");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_MessageFeedbacks_CreatedAt");
            entity.HasIndex(e => new { e.TenantId, e.Rating }).HasDatabaseName("IX_MessageFeedbacks_Tenant_Rating");
        });
        
        // ===== ADMIN CONFIGURATION =====
        modelBuilder.Entity<AdminConfiguration>(entity =>
        {
            entity.ToTable("AdminConfigurations");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ModifiedBy).HasMaxLength(100);
            entity.Property(e => e.ValidationRule).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Unique key ensures no duplicate configuration keys
            entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_AdminConfigurations_Key");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_AdminConfigurations_Category");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_AdminConfigurations_IsActive");
        });
        
        // ===== ADMIN USER =====
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastLoginAt);
            entity.Property(e => e.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.LockedUntil);
            
            // Unique username
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_AdminUsers_TenantId");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_AdminUsers_Username");
            entity.HasIndex(e => e.Email).HasDatabaseName("IX_AdminUsers_Email");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_AdminUsers_IsActive");
            entity.HasIndex(e => new { e.TenantId, e.Username }).HasDatabaseName("IX_AdminUsers_Tenant_Username");
        });
        
        // ===== API KEY =====
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.RateLimitPerMinute).IsRequired().HasDefaultValue(20);
            entity.Property(e => e.RateLimitPerDay).IsRequired().HasDefaultValue(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.LastUsedAt);
            entity.Property(e => e.UsageCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.CreatedBy);
            
            // Indexes for lookups
            entity.HasIndex(e => e.KeyHash).IsUnique().HasDatabaseName("IX_ApiKeys_KeyHash");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ApiKeys_TenantId");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApiKeys_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ApiKeys_CreatedAt");
        });
        
        // ===== TENANT =====
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PlanTier).IsRequired().HasMaxLength(50).HasDefaultValue("Free");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CustomDomain).HasMaxLength(200);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20).HasDefaultValue("#667eea");
            entity.Property(e => e.MaxDocuments).IsRequired().HasDefaultValue(10);
            entity.Property(e => e.MaxMonthlyMessages).IsRequired().HasDefaultValue(1000);
            entity.Property(e => e.CurrentDocumentCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.CurrentMonthMessages).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.BillingPeriodStart).IsRequired();
            entity.Property(e => e.SettingsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastActivityAt);
            entity.Property(e => e.SubscriptionExpiresAt);
            
            // Unique constraints
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("IX_Tenants_Slug");
            entity.HasIndex(e => e.CustomDomain).IsUnique().HasDatabaseName("IX_Tenants_CustomDomain");
            entity.HasIndex(e => e.Email).HasDatabaseName("IX_Tenants_Email");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Tenants_IsActive");
            entity.HasIndex(e => e.PlanTier).HasDatabaseName("IX_Tenants_PlanTier");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Tenants_CreatedAt");
        });
        
        // ===== TENANT SETTINGS =====
        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.ToTable("TenantSettings");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.VectorStorageMode).IsRequired().HasMaxLength(20).HasDefaultValue("SQL");
            entity.Property(e => e.QdrantCollectionName).HasMaxLength(100);
            entity.Property(e => e.EnableDocumentChunking).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ChunkSize).IsRequired().HasDefaultValue(512);
            entity.Property(e => e.ChunkOverlap).IsRequired().HasDefaultValue(50);
            entity.Property(e => e.ChatHistoryRetentionDays).IsRequired().HasDefaultValue(90);
            entity.Property(e => e.EnableFeedback).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(500);
            entity.Property(e => e.ChatPlaceholder).HasMaxLength(200).HasDefaultValue("Ask me anything...");
            entity.Property(e => e.EnableTools).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Temperature).IsRequired().HasDefaultValue(0.7);
            entity.Property(e => e.MaxTokens).IsRequired().HasDefaultValue(1000);
            entity.Property(e => e.SystemPrompt).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            
            // One-to-one with Tenant
            entity.HasOne(s => s.Tenant)
                .WithOne(t => t.Settings)
                .HasForeignKey<TenantSettings>(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.TenantId).IsUnique().HasDatabaseName("IX_TenantSettings_TenantId");
        });
        
        // ===== GLOBAL QUERY FILTERS (Multi-Tenancy) =====
        // Automatically filter all queries by current tenant
        if (_tenantContext != null)
        {
            modelBuilder.Entity<KnowledgeDocument>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<ChatSession>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<ChatMessage>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<MessageFeedback>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<AdminUser>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
            // ApiKey.TenantId is string, so convert for comparison
            modelBuilder.Entity<ApiKey>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId.ToString());
        }
    }
}
