using ChatAI.Application.Interfaces;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial test data
/// </summary>
public class DbSeeder
{
    private readonly ChatDbContext _context;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(ChatDbContext context, ILogger<DbSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if database has any data
            if (await _context.KnowledgeDocuments.AnyAsync())
            {
                _logger.LogInformation("Database already seeded, skipping");
                return;
            }

            _logger.LogInformation("Seeding database with initial data...");

            await SeedKnowledgeBaseAsync();
            await SeedTestConversationAsync();

            await _context.SaveChangesAsync();

            _logger.LogInformation("✓ Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    /// <summary>
    /// Seed knowledge base with sample documents
    /// </summary>
    private async Task SeedKnowledgeBaseAsync()
    {
        var knowledgeDocs = new List<KnowledgeDocument>
        {
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = "Company Return Policy",
                Content = @"Our company offers a generous 30-day return policy for all products. 
                
                Return Conditions:
                - Products must be in original condition
                - Original packaging required
                - Receipt or proof of purchase needed
                - No returns on opened software or digital products
                
                Refund Process:
                - Full refund within 30 days of purchase
                - Partial refund (50%) between 31-60 days
                - Store credit only after 60 days
                
                To initiate a return, contact customer service at returns@company.com or call 1-800-RETURNS.",
                Category = "policy",
                Source = "company-handbook-2025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = "Product Warranty Information",
                Content = @"All products come with a standard 1-year manufacturer warranty.
                
                Warranty Coverage:
                - Manufacturing defects
                - Material failures
                - Hardware malfunctions (for electronics)
                
                Not Covered:
                - Accidental damage
                - Water damage
                - Normal wear and tear
                - Unauthorized modifications
                
                Extended Warranty:
                - 2-year extended warranty available for $49.99
                - 3-year premium warranty available for $99.99
                
                To file a warranty claim, visit warranty.company.com with your serial number and purchase date.",
                Category = "policy",
                Source = "warranty-guide-2025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = "Shipping and Delivery",
                Content = @"We offer multiple shipping options to meet your needs:
                
                Shipping Options:
                - Standard Shipping (5-7 business days): FREE on orders over $50
                - Express Shipping (2-3 business days): $9.99
                - Next Day Delivery: $19.99
                - International Shipping: Varies by country
                
                Order Tracking:
                - Track your order at tracking.company.com
                - Tracking number sent via email within 24 hours of shipment
                
                Delivery Issues:
                - Contact support@company.com for missing packages
                - Claims must be filed within 14 days of expected delivery
                
                Free shipping applies to continental US only. Some restrictions apply.",
                Category = "shipping",
                Source = "shipping-policy-2025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = "Technical Support FAQ",
                Content = @"Frequently Asked Questions about our technical support:
                
                Q: How do I reset my password?
                A: Visit account.company.com and click 'Forgot Password'. Follow the email instructions.
                
                Q: What are your support hours?
                A: Monday-Friday 8AM-8PM EST, Saturday 9AM-5PM EST. Closed Sundays.
                
                Q: How can I contact technical support?
                A: Email: techsupport@company.com, Phone: 1-800-TECH-SUP, Live Chat on our website
                
                Q: Do you offer phone support?
                A: Yes, phone support is available during business hours for all customers.
                
                Q: Is remote assistance available?
                A: Yes, our technicians can remotely access your device with your permission.
                
                Q: What information should I have ready when contacting support?
                A: Product model number, serial number, purchase date, and description of the issue.",
                Category = "support",
                Source = "faq-technical-2025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                Title = "Account Management Guide",
                Content = @"Managing your company account is easy and secure.
                
                Creating an Account:
                - Go to account.company.com/register
                - Provide email, password, and basic information
                - Verify your email address
                
                Account Benefits:
                - Order history and tracking
                - Saved shipping addresses
                - Wishlist and favorites
                - Exclusive member discounts
                - Priority customer support
                
                Security Features:
                - Two-factor authentication (2FA) available
                - Password must be 8+ characters with numbers and symbols
                - Account activity monitoring
                
                Privacy:
                - We never share your data with third parties
                - View our privacy policy at company.com/privacy
                - Delete your account anytime at account.company.com/settings",
                Category = "account",
                Source = "account-guide-2025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.KnowledgeDocuments.AddRange(knowledgeDocs);
        _logger.LogInformation("Added {Count} knowledge documents", knowledgeDocs.Count);
    }

    /// <summary>
    /// Seed a test conversation for demo purposes
    /// </summary>
    private async Task SeedTestConversationAsync()
    {
        var sessionId = Guid.NewGuid().ToString();
        var userId = "demo-user";

        var session = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            Title = "Demo Conversation - Product Return Question",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                Role = MessageRole.User,
                Content = "Hi! I bought a product last week and I'm not happy with it. Can I return it?",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                Role = MessageRole.Assistant,
                Content = "Hello! Yes, you can absolutely return it. According to our company policy, we offer a generous 30-day return policy for all products. Since you purchased it just last week, you're well within the return window. To initiate the return, you'll need:\n\n1. The product in original condition\n2. Original packaging\n3. Your receipt or proof of purchase\n\nYou can contact our customer service at returns@company.com or call 1-800-RETURNS to start the process. You'll receive a full refund!",
                Timestamp = DateTime.UtcNow.AddHours(-2).AddMinutes(1)
            },
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                Role = MessageRole.User,
                Content = "Great! How long will it take to get my refund?",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                Role = MessageRole.Assistant,
                Content = "Once we receive your returned item, refunds are typically processed within 5-7 business days. The refund will be credited back to your original payment method. You'll receive an email confirmation once the refund has been processed.",
                Timestamp = DateTime.UtcNow.AddHours(-1).AddMinutes(1)
            }
        };

        _context.ChatSessions.Add(session);
        _context.ChatMessages.AddRange(messages);
        
        _logger.LogInformation("Added test conversation with {Count} messages", messages.Count);
    }
}
