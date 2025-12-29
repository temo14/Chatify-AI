// Quick tool to seed test tenants
// Run: dotnet run --project SeedTestTenants

using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.Services;
using ChatAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("../ChatAI.Api/appsettings.Development.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not found");

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString)
    .Options;

var context = new ApplicationDbContext(options);
var authService = new AuthService(configuration, null);

Console.WriteLine("Creating test tenants...");

// Create Tenant A
var tenantA = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "tenanta");
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
        PasswordHash = authService.HashPassword("Password123!"),
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
        WelcomeMessage = "Welcome to Tenant A!",
        Temperature = 0.7f,
        MaxTokens = 2000,
        EnableTools = true,
        CreatedAt = DateTime.UtcNow
    };

    context.Tenants.Add(tenantA);
    context.AdminUsers.Add(adminA);
    context.TenantSettings.Add(settingsA);
    Console.WriteLine("✓ Created Tenant A (adminA / Password123!)");
}
else
{
    Console.WriteLine("Tenant A already exists");
}

// Create Tenant B
var tenantB = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "tenantb");
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
        PasswordHash = authService.HashPassword("Password123!"),
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
        WelcomeMessage = "Welcome to Tenant B!",
        Temperature = 0.8f,
        MaxTokens = 3000,
        EnableTools = true,
        CreatedAt = DateTime.UtcNow
    };

    context.Tenants.Add(tenantB);
    context.AdminUsers.Add(adminB);
    context.TenantSettings.Add(settingsB);
    Console.WriteLine("✓ Created Tenant B (adminB / Password123!)");
}
else
{
    Console.WriteLine("Tenant B already exists");
}

await context.SaveChangesAsync();

// Display all tenants
var allTenants = await context.Tenants.Where(t => t.IsActive).ToListAsync();
Console.WriteLine("\nActive Tenants:");
foreach (var tenant in allTenants)
{
    Console.WriteLine($"  - {tenant.Slug}: {tenant.Name} ({tenant.PlanTier})");
}

Console.WriteLine("\n✅ Test tenants ready for multi-tenant testing!");
