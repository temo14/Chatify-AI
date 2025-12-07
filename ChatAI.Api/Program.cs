using ChatAI.Api.Extensions;
using ChatAI.Api.Middleware;
using AspNetCoreRateLimit;
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
    builder.Services.AddMemoryCache(options =>
    {
        // Don't set SizeLimit to avoid cache entry size requirement
        options.CompactionPercentage = 0.25;
    });
    builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

    builder.Services.AddControllers();

    builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
    builder.Services.AddAzureOpenAIServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddHealthCheckServices(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    await app.UseDatabaseMigrationsAsync(app.Environment);

    // Global exception handler - must be first in pipeline
    app.UseGlobalExceptionHandler();

    // Temporarily disable rate limiting for testing
    // app.UseIpRateLimiting();
    app.UseHttpsRedirection();
    
    // Serve static files (HTML, CSS, JS) from wwwroot folder
    app.UseStaticFiles();
    
    app.UseSwaggerDocumentation(app.Environment);
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
