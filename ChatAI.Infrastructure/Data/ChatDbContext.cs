using ChatAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Data;

/// <summary>
/// Database context for Chatify AI
/// Simple schema: Chat sessions/messages + Knowledge base for RAG
/// </summary>
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }
    
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
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ChatSessions_CreatedAt");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatSessions_UserId");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ChatSessions_IsActive");
            entity.HasIndex(e => e.LastActivityAt).HasDatabaseName("IX_ChatSessions_LastActivity");
            
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
            entity.HasIndex(e => e.SessionId).HasDatabaseName("IX_ChatMessages_SessionId");
            entity.HasIndex(e => new { e.SessionId, e.Timestamp }).HasDatabaseName("IX_ChatMessages_Session_Time");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_ChatMessages_Timestamp");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatMessages_UserId");
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
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)"); // JSON
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            
            // Query indexes (for control panel and RAG search)
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_KnowledgeDocuments_Category");
            entity.HasIndex(e => e.Source).HasDatabaseName("IX_KnowledgeDocuments_Source");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_KnowledgeDocuments_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_KnowledgeDocuments_CreatedAt");
            entity.HasIndex(e => new { e.Category, e.IsActive }).HasDatabaseName("IX_KnowledgeDocuments_Category_Active");
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
            entity.HasIndex(e => e.MessageId).HasDatabaseName("IX_MessageFeedbacks_MessageId");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("IX_MessageFeedbacks_SessionId");
            entity.HasIndex(e => e.Rating).HasDatabaseName("IX_MessageFeedbacks_Rating");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_MessageFeedbacks_CreatedAt");
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
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_AdminUsers_Username");
            entity.HasIndex(e => e.Email).HasDatabaseName("IX_AdminUsers_Email");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_AdminUsers_IsActive");
        });
        
        // ===== API KEY =====
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
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
            entity.HasIndex(e => e.ClientId).HasDatabaseName("IX_ApiKeys_ClientId");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApiKeys_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ApiKeys_CreatedAt");
        });
    }
}
