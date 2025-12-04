using ChatAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatAI.Api.Extensions;

/// <summary>
/// Extension methods for configuring the application middleware pipeline
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Run database migrations and seed data (Development only)
    /// </summary>
    public static async Task<IApplicationBuilder> UseDatabaseMigrationsAsync(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return app;
        }

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Running database migrations...");
        await db.Database.MigrateAsync();

        logger.LogInformation("Seeding database with test data...");
        var seeder = new DbSeeder(db, scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>());
        await seeder.SeedAsync();

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
}
