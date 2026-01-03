using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Infrastructure.Interfaces;
using OpenAI.Embeddings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAI.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data on first run
/// Creates: Dott tenant, Platform admin user, TenantSettings, Demo knowledge
/// </summary>
public class DbSeeder
{
    private readonly ChatDbContext _context;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DbSeeder(
        ChatDbContext context, 
        IAuthService authService,
        IConfiguration configuration,
        ILogger<DbSeeder> logger,
        IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task SeedAsync()
    {
        try
        {
            // 1. Seed Dott tenant with platform admin user (in one transaction)
            var dottTenant = await SeedDottTenantAsync();

            // 2. Seed test tenants for multi-tenant isolation testing (development only)
            if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
            {
                await SeedTestTenantsAsync();
            }

            // 3. Seed platform-level admin configurations
            await SeedAdminConfigurationsAsync();

            // Check if database already has knowledge data
            // Must ignore query filters since TenantContext isn't set during seeding
            if (await _context.KnowledgeDocuments.IgnoreQueryFilters().AnyAsync())
            {
                _logger.LogInformation("Database already seeded with knowledge, skipping");
                return;
            }

            _logger.LogInformation("Seeding database with demo data...");

            // 4. Seed demo knowledge base for testing ChatifyAI
            await SeedDemoKnowledgeAsync(dottTenant.Id);

            await _context.SaveChangesAsync();
            
            // 5. Generate embeddings for demo documents (async, non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000); // Wait for app startup to complete
                    
                    using var scope = _serviceProvider.CreateScope();
                    var embeddingClient = scope.ServiceProvider.GetRequiredService<EmbeddingClient>();
                    var vectorStorageFactory = scope.ServiceProvider.GetRequiredService<IVectorStorageFactory>();
                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    
                    // Set tenant context for Dott tenant
                    tenantContext.SetTenant(dottTenant.Id, dottTenant.Slug);
                    
                    // Get document IDs first
                    List<Guid> docIds;
                    using (var readContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>())
                    {
                        docIds = await readContext.KnowledgeDocuments
                            .Where(d => d.TenantId == dottTenant.Id && string.IsNullOrEmpty(d.EmbeddingReference))
                            .Select(d => d.Id)
                            .ToListAsync();
                    }
                    
                    if (docIds.Any())
                    {
                        _logger.LogInformation("Generating embeddings for {Count} demo documents (background task)...", docIds.Count);
                        
                        var vectorStorage = await vectorStorageFactory.CreateForCurrentTenantAsync();
                        
                        foreach (var docId in docIds)
                        {
                            try
                            {
                                // Use fresh context for each document to avoid tracking issues
                                using var docContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
                                var doc = await docContext.KnowledgeDocuments.FindAsync(docId);
                                if (doc == null) continue;
                                
                                var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(doc.Content);
                                var embedding = embeddingResponse.Value.ToFloats().ToArray();
                                
                                var metadata = new Dictionary<string, object>
                                {
                                    { "title", doc.Title },
                                    { "category", doc.Category ?? "general" },
                                    { "source", doc.Source ?? "unknown" }
                                };
                                
                                await vectorStorage.StoreEmbeddingAsync(doc.Id, embedding, metadata);
                                
                                doc.EmbeddingReference = $"vector:{doc.Id}";
                                await docContext.SaveChangesAsync();
                                
                                _logger.LogDebug("✓ Generated embedding for: {Title}", doc.Title);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to generate embedding for document {Id}", docId);
                            }
                        }
                        
                        _logger.LogInformation("✓ Embedding generation completed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embeddings for demo documents");
                }
            });

            _logger.LogInformation("✓ Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    /// <summary>
    /// Seeds the Dott tenant - YOUR operational tenant for platform administration
    /// Platform admins belong to this tenant and can manage all customer tenants
    /// Also used for testing ChatifyAI with demo knowledge
    /// </summary>
    private async Task<Tenant> SeedDottTenantAsync()
    {
        // Check if Dott tenant exists
        var existingTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == "dott");
        if (existingTenant != null)
        {
            _logger.LogInformation("Dott tenant already exists");
            return existingTenant;
        }

        // Get credentials from configuration or use defaults
        var username = _configuration["Admin:Username"] ?? "admin";
        var password = _configuration["Admin:Password"] ?? "Admin@123456";
        var email = _configuration["Admin:Email"] ?? "admin@chatify.ge";

        // Create IDs upfront to avoid circular dependency
        var tenantId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        // Create the platform admin user with the correct TenantId
        var platformAdmin = new AdminUser
        {
            Id = adminUserId,
            Username = username,
            PasswordHash = _authService.HashPassword(password),
            Email = email,
            FullName = "Platform Administrator",
            TenantId = tenantId,
            IsPlatformAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminUsers.Add(platformAdmin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Created platform admin user: {Username}", username);

        // Now create the Dott tenant (admin user already references it via TenantId)
        var dottTenant = new Tenant
        {
            Id = tenantId,
            Slug = "dott",
            Name = "Dott - Platform Administration",
            Email = email,
            PlanTier = "Internal",
            IsActive = true,
            MaxDocuments = 1000,
            MaxMonthlyMessages = 999999,
            CurrentDocumentCount = 0,
            CurrentMonthMessages = 0,
            BillingPeriodStart = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = dottTenant.Id,
            VectorStorageMode = "SQL",
            EnableDocumentChunking = true,
            EnableChatHistory = true,
            ChatHistoryRetentionDays = 365,
            EnableFeedback = true,
            EnableOverview = true,
            EnableEmailSupport = true,
            WelcomeMessage = "Welcome to ChatifyAI! How can I help you today?",
            Temperature = 0.7f,
            MaxTokens = 2000,
            EnableTools = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(dottTenant);
        _context.TenantSettings.Add(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Created Dott tenant: {TenantSlug}", dottTenant.Slug);
        return dottTenant;
    }

    /// <summary>
    /// Seeds the platform admin user - YOUR account for managing the platform
    /// Only platform admins can create and manage customer tenants
    /// </summary>
    private async Task SeedPlatformAdminAsync(Guid dottTenantId)
    {
        // Check if platform admin already exists
        // Must ignore query filters since TenantContext isn't set during seeding
        var existingAdmin = await _context.AdminUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IsPlatformAdmin && u.TenantId == dottTenantId);
        
        if (existingAdmin != null)
        {
            _logger.LogInformation("Platform admin already exists");
            return;
        }

        // Get credentials from configuration or use defaults
        var username = _configuration["Admin:Username"] ?? "admin";
        var password = _configuration["Admin:Password"] ?? "Admin@123456";
        var email = _configuration["Admin:Email"] ?? "admin@chatify.ge";

        var platformAdmin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = _authService.HashPassword(password),
            Email = email,
            FullName = "Platform Administrator",
            TenantId = dottTenantId,
            IsPlatformAdmin = true, // Critical: Enables tenant management
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdminUsers.Add(platformAdmin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Created platform admin: {Username}", username);
    }

    /// <summary>
    /// Seed demo knowledge base for testing ChatifyAI functionality
    /// These are example documents to demonstrate RAG capabilities
    /// </summary>
    private async Task SeedDemoKnowledgeAsync(Guid tenantId)
    {
        var knowledgeDocs = new List<KnowledgeDocument>
        {
            new KnowledgeDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
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
        _logger.LogInformation("Added {Count} demo knowledge documents for Dott tenant", knowledgeDocs.Count);
    }

    /// <summary>
    /// Seeds platform-level admin configurations (system defaults)
    /// These are global settings used across the platform
    /// </summary>
    private async Task SeedAdminConfigurationsAsync()
    {
        // Check if admin configurations already exist
        if (await _context.AdminConfigurations.AnyAsync())
        {
            _logger.LogInformation("AdminConfigurations already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var adminConfigs = new List<AdminConfiguration>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AI.DefaultSystemPrompt",
                Value = @"You are ChatifyAI, an intelligent conversational assistant.

                ## CORE ABILITIES
                - Knowledge Base Access: Search and retrieve information from integrated knowledge repository
                - Email Support Tool: Send detailed support emails to administrators when needed
                - Conversation Memory: Maintain context across conversations

                ## OPERATING PRINCIPLES
                1. ACCURACY FIRST: Base responses on knowledge base, clearly state limitations
                2. CLARITY: Use clear formatting, adapt technical depth to user expertise
                3. PROACTIVE: Anticipate follow-up questions, offer to escalate issues
                4. PROFESSIONAL: Friendly yet professional tone, respect privacy",
                DataType = "String",
                Category = "AI",
                Description = "Default system prompt for AI conversations",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AI.DefaultTemperature",
                Value = "0.7",
                DataType = "Float",
                Category = "AI",
                Description = "Default temperature for AI responses (0.0-1.0)",
                ValidationRule = "range:0,1",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AI.DefaultMaxTokens",
                Value = "2000",
                DataType = "Integer",
                Category = "AI",
                Description = "Default maximum tokens for AI responses",
                ValidationRule = "range:100,4000",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Features.EmailSupportEnabled",
                Value = "true",
                DataType = "Boolean",
                Category = "Features",
                Description = "Enable/disable email support tool globally",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Features.DocumentChunkingEnabled",
                Value = "true",
                DataType = "Boolean",
                Category = "Features",
                Description = "Enable/disable document chunking for large documents",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "VectorStorage.QdrantThreshold",
                Value = "100",
                DataType = "Integer",
                Category = "VectorStorage",
                Description = "Document count threshold for switching from SQL to Qdrant",
                ValidationRule = "min:1",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Security.SessionExpirationMinutes",
                Value = "60",
                DataType = "Integer",
                Category = "Security",
                Description = "JWT session expiration in minutes",
                ValidationRule = "range:5,1440",
                IsActive = true,
                ModifiedBy = "System",
                CreatedAt = now
            }
        };

        _context.AdminConfigurations.AddRange(adminConfigs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Seeded {Count} platform admin configurations", adminConfigs.Count);
    }

    /// <summary>
    /// Seeds test tenants for multi-tenant isolation testing (Development only)
    /// Creates Tenant A and Tenant B with admin users for testing cross-tenant isolation
    /// </summary>
    private async Task SeedTestTenantsAsync()
    {
        // Create Tenant A
        var tenantA = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == "tenanta");
        if (tenantA == null)
        {
            var tenantAId = Guid.NewGuid();
            tenantA = new Tenant
            {
                Id = tenantAId,
                Slug = "tenanta",
                Name = "Test Tenant A",
                Email = "admin@tenanta.com",
                PlanTier = "Basic",
                IsActive = true,
                MaxDocuments = 100,
                MaxMonthlyMessages = 10000,
                CurrentDocumentCount = 0,
                CurrentMonthMessages = 0,
                BillingPeriodStart = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var adminA = new AdminUser
            {
                Id = Guid.NewGuid(),
                Username = "adminA",
                PasswordHash = _authService.HashPassword("Password123!"),
                Email = "admin@tenanta.com",
                FullName = "Admin User A",
                TenantId = tenantAId,
                IsPlatformAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var settingsA = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAId,
                VectorStorageMode = "SQL",
                EnableDocumentChunking = true,
                EnableChatHistory = true,
                ChatHistoryRetentionDays = 90,
                EnableFeedback = true,
                EnableOverview = true,
                WelcomeMessage = "Welcome to Tenant A! How can I help you?",
                Temperature = 0.7f,
                MaxTokens = 2000,
                EnableTools = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenantA);
            _context.AdminUsers.Add(adminA);
            _context.TenantSettings.Add(settingsA);
            _logger.LogInformation("✓ Created Tenant A (tenanta) with admin user adminA");
        }
        else
        {
            _logger.LogInformation("Test Tenant A already exists");
        }

        // Create Tenant B
        var tenantB = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == "tenantb");
        if (tenantB == null)
        {
            var tenantBId = Guid.NewGuid();
            tenantB = new Tenant
            {
                Id = tenantBId,
                Slug = "tenantb",
                Name = "Test Tenant B",
                Email = "admin@tenantb.com",
                PlanTier = "Pro",
                IsActive = true,
                MaxDocuments = 500,
                MaxMonthlyMessages = 50000,
                CurrentDocumentCount = 0,
                CurrentMonthMessages = 0,
                BillingPeriodStart = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var adminB = new AdminUser
            {
                Id = Guid.NewGuid(),
                Username = "adminB",
                PasswordHash = _authService.HashPassword("Password123!"),
                Email = "admin@tenantb.com",
                FullName = "Admin User B",
                TenantId = tenantBId,
                IsPlatformAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var settingsB = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantBId,
                VectorStorageMode = "SQL",
                EnableDocumentChunking = true,
                EnableChatHistory = true,
                ChatHistoryRetentionDays = 180,
                EnableFeedback = true,
                EnableOverview = true,
                WelcomeMessage = "Welcome to Tenant B! How can I assist you today?",
                Temperature = 0.8f,
                MaxTokens = 3000,
                EnableTools = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenantB);
            _context.AdminUsers.Add(adminB);
            _context.TenantSettings.Add(settingsB);
            _logger.LogInformation("✓ Created Tenant B (tenantb) with admin user adminB");
        }
        else
        {
            _logger.LogInformation("Test Tenant B already exists");
        }

        await _context.SaveChangesAsync();
    }
}

