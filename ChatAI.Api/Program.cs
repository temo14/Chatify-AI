using AspNetCoreRateLimit;
using ChatAI.Api.Extensions;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Azure Key Vault for Production (loads secrets before services are configured)
    // Note: If Key Vault is configured but inaccessible, the app will fail to start (fail-fast principle)
    // This ensures we don't run with potentially missing critical secrets
    builder.AddAzureKeyVaultConfiguration();

    // Reconfigure Serilog with Seq support now that configuration is loaded
    builder.ConfigureSerilogWithSeq();

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add memory cache and rate limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

    builder.Services.AddControllers();

    builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
    builder.Services.AddAzureOpenAIServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddHealthCheckServices(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    // Apply database migrations and seed initial data
    await app.UseDatabaseMigrationsAsync(app.Environment);

    // Global exception handler - must be first in pipeline
    app.UseGlobalExceptionHandler();

    app.UseIpRateLimiting();
    app.UseHttpsRedirection();
    
    // Serve static files (HTML, CSS, JS) from wwwroot folder
    app.UseStaticFiles();
    
    app.UseSwaggerDocumentation(app.Environment);
    
    // Authentication and authorization
    app.UseAuthentication();
    
    // Multi-tenancy middleware - MUST be after UseAuthentication() but before UseAuthorization()
    app.UseTenantResolution();
    
    app.UseAuthorization();
    
    // Map health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    
    app.MapControllers();

    Log.Information("ChatAI application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ChatAI application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so tests can reference it
public partial class Program { }
