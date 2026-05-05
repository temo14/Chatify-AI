using AspNetCoreRateLimit;
using ChatAI.Api.Extensions;
using Serilog;
using Serilog.Events;

try
{
    Log.Information("Starting ChatAI application...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Reconfigure Serilog with full configuration (Console + File + Seq)
    // Replaces bootstrap logger with production-ready multi-sink configuration
    builder.ConfigureSerilogWithSeq();

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add memory cache and rate limiting
    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache(); // For OAuth state storage
    builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

    builder.Services.AddControllers();

    builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
    builder.Services.AddAzureOpenAIServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddMetaChannelsServices(builder.Configuration);
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddHealthCheckServices(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    // Configure forwarded headers for Azure Container Apps proxy (HTTPS detection)
    app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                          Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });

    // Apply database migrations and seed initial data
    //await app.UseDatabaseMigrationsAsync(app.Environment);

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

    Log.Information("✅ ChatAI application configured successfully");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Listening on: {Urls}", string.Join(", ", app.Urls));
    
    app.Run();
    
    Log.Information("ChatAI application stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Shutting down logging...");
    Log.CloseAndFlush();
}

// Make the implicit Program class public so tests can reference it
public partial class Program { }
