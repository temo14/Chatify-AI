using ChatAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Infrastructure.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }
    
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    
    public DbSet<UserMemory> UserMemories { get; set; } = null!;
    
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("ChatSessions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(500);
            
            // Indexes for query performance
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatSessions_UserId");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ChatSessions_CreatedAt");
            entity.HasIndex(e => new { e.UserId, e.IsActive }).HasDatabaseName("IX_ChatSessions_UserId_IsActive");
            
            // Ignore Messages collection (will be loaded separately)
            entity.Ignore(e => e.Messages);
            entity.Ignore(e => e.MessageCount);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            
            // Convert enum to string in database
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            // Tool-related properties
            entity.Property(e => e.IsToolCall).IsRequired();
            entity.Property(e => e.ToolName).HasMaxLength(100);
            entity.Property(e => e.ToolArguments);
            entity.Property(e => e.ToolResult);
            entity.Property(e => e.EmbeddingReference).HasMaxLength(200);
            
            // Indexes
            entity.HasIndex(e => e.SessionId).HasDatabaseName("IX_ChatMessages_SessionId");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ChatMessages_UserId");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_ChatMessages_Timestamp");
            entity.HasIndex(e => new { e.SessionId, e.Timestamp }).HasDatabaseName("IX_ChatMessages_Session_Time");
        });

        // UserMemory configuration
        modelBuilder.Entity<UserMemory>(entity =>
        {
            entity.ToTable("UserMemories");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Enum conversion
            entity.Property(e => e.Importance)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            
            entity.Property(e => e.EmbeddingReference).HasMaxLength(200);
            entity.Property(e => e.RelevanceScore).HasColumnType("decimal(5,4)");
            
            // Indexes
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserMemories_UserId");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_UserMemories_Category");
            entity.HasIndex(e => e.Importance).HasDatabaseName("IX_UserMemories_Importance");
            entity.HasIndex(e => new { e.UserId, e.Category }).HasDatabaseName("IX_UserMemories_User_Category");
        });

        // KnowledgeDocument configuration (RAG base knowledge)
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.ToTable("KnowledgeDocuments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.EmbeddingReference).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.MetadataJson);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            
            // Indexes for fast retrieval
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_KnowledgeDocuments_Category");
            entity.HasIndex(e => e.Source).HasDatabaseName("IX_KnowledgeDocuments_Source");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_KnowledgeDocuments_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_KnowledgeDocuments_CreatedAt");
            entity.HasIndex(e => new { e.Category, e.IsActive }).HasDatabaseName("IX_KnowledgeDocuments_Category_Active");
        });
    }
}
