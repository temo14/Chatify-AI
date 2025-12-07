using ChatAI.Domain.Entities;
using ChatAI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Tests.Integration;

/// <summary>
/// Integration tests for RAG (Retrieval-Augmented Generation) functionality
/// These tests verify the knowledge base CRUD operations
/// </summary>
[Trait("Category", "Integration")]
public class RagIntegrationTests : IDisposable
{
    private readonly ChatDbContext _context;

    public RagIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
    }

    [Fact]
    public async Task AddKnowledgeDocument_SavesSuccessfully()
    {
        // Arrange
        var doc = new KnowledgeDocument
        {
            Title = "Test Document",
            Content = "This is test content",
            Category = "test",
            IsActive = true
        };

        // Act
        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.KnowledgeDocuments.FindAsync(doc.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Document");
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetKnowledgeDocuments_FiltersActiveOnly()
    {
        // Arrange
        var activeDoc = new KnowledgeDocument
        {
            Title = "Active",
            Content = "Active content",
            IsActive = true
        };

        var inactiveDoc = new KnowledgeDocument
        {
            Title = "Inactive",
            Content = "Inactive content",
            IsActive = false
        };

        _context.KnowledgeDocuments.AddRange(activeDoc, inactiveDoc);
        await _context.SaveChangesAsync();

        // Act
        var activeOnly = await _context.KnowledgeDocuments
            .Where(d => d.IsActive)
            .ToListAsync();

        // Assert
        activeOnly.Should().HaveCount(1);
        activeOnly[0].Title.Should().Be("Active");
    }

    [Fact]
    public async Task GetKnowledgeDocuments_FiltersByCategory()
    {
        // Arrange
        var supportDoc = new KnowledgeDocument
        {
            Title = "Support Doc",
            Content = "Support content",
            Category = "support",
            IsActive = true
        };

        var productDoc = new KnowledgeDocument
        {
            Title = "Product Doc",
            Content = "Product content",
            Category = "product",
            IsActive = true
        };

        _context.KnowledgeDocuments.AddRange(supportDoc, productDoc);
        await _context.SaveChangesAsync();

        // Act
        var supportDocs = await _context.KnowledgeDocuments
            .Where(d => d.Category == "support")
            .ToListAsync();

        // Assert
        supportDocs.Should().HaveCount(1);
        supportDocs[0].Title.Should().Be("Support Doc");
    }

    [Fact]
    public async Task UpdateKnowledgeDocument_ModifiesSuccessfully()
    {
        // Arrange
        var doc = new KnowledgeDocument
        {
            Title = "Original Title",
            Content = "Original Content",
            IsActive = true
        };

        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Act
        doc.Title = "Updated Title";
        doc.Content = "Updated Content";
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.KnowledgeDocuments.FindAsync(doc.Id);
        updated!.Title.Should().Be("Updated Title");
        updated.Content.Should().Be("Updated Content");
    }

    [Fact]
    public async Task DeleteKnowledgeDocument_RemovesSuccessfully()
    {
        // Arrange
        var doc = new KnowledgeDocument
        {
            Title = "To Delete",
            Content = "Will be deleted",
            IsActive = true
        };

        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Act
        _context.KnowledgeDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.KnowledgeDocuments.FindAsync(doc.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task KnowledgeDocument_SupportsMetadata()
    {
        // Arrange
        var doc = new KnowledgeDocument
        {
            Title = "Doc with Metadata",
            Content = "Content",
            MetadataJson = "{\"author\":\"John\",\"version\":\"1.0\"}",
            IsActive = true
        };

        // Act
        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.KnowledgeDocuments.FindAsync(doc.Id);
        saved!.MetadataJson.Should().Contain("John");
        saved.MetadataJson.Should().Contain("1.0");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
