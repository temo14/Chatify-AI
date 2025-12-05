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
    }
}
