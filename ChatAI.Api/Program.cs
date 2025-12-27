using AspNetCoreRateLimit;
using ChatAI.Api.Extensions;
using Serilog;

// Configure Serilog before building the application
LoggingExtensions.ConfigureSerilog();

try
{
    Log.Information("Starting ChatAI application");

    var builder = WebApplication.CreateBuilder(args);

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
