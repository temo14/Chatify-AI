using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Api.Extensions;

/// <summary>
/// Extension methods for configuring the application middleware pipeline
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Run database migrations and seed data
    /// </summary>
    /// <param name="runMigrationsInProduction">Set to true to run migrations in production (use with caution)</param>
    public static async Task<IApplicationBuilder> UseDatabaseMigrationsAsync(
        this IApplicationBuilder app,
        IWebHostEnvironment environment,
        bool runMigrationsInProduction = false)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Skip migrations entirely in test environment
        if (environment.IsEnvironment("Testing"))
        {
            logger.LogInformation("Test environment detected. Skipping database migrations.");
            return app;
        }
        
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        // Skip migrations for InMemory database (used in tests)
        var providerName = db.Database.ProviderName;
        if (providerName != null && providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("InMemory database detected. Skipping migrations.");
            return app;
        }

        // In production, only run migrations if explicitly enabled
        if (environment.IsProduction() && !runMigrationsInProduction)
        {
            logger.LogInformation("Production environment detected. Skipping automatic migrations. Run migrations manually using: dotnet ef database update");
            return app;
        }

        // Run SQL Server migrations
        logger.LogInformation("Running database migrations...");
        await db.Database.MigrateAsync();

        // Seed data (only in development or first run)
        if (environment.IsDevelopment())
        {
            logger.LogInformation("Seeding database...");
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var seeder = new DbSeeder(
                db, 
                authService, 
                configuration, 
                scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>(),
                scope.ServiceProvider);
            await seeder.SeedAsync();
        }

        return app;
    }

    /// <summary>
    /// Configure Swagger UI (Development only)
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chatify AI API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Chatify AI API Documentation";
        });

        return app;
    }

    /// <summary>
    /// Enable global exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ChatAI.Api.Middleware.GlobalExceptionMiddleware>();
    }

    /// <summary>
    /// Enable multi-tenancy middleware for tenant resolution
    /// IMPORTANT: Must be called after UseAuthentication() and before UseAuthorization()
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ChatAI.Api.Middleware.TenantResolutionMiddleware>();
    }
}
