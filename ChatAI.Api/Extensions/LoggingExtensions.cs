using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace ChatAI.Api.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging
/// Supports: Console, File, and Seq (with Web UI for log querying)
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Reconfigure Serilog with Seq sink after configuration is loaded.
    /// 
    /// Seq provides a web UI for searching, filtering, and analyzing logs.
    /// Configure via appsettings.json or environment variables:
    /// - Seq__ServerUrl: Seq server URL (e.g., https://seq.yourcompany.com or http://localhost:5341)
    /// - Seq__ApiKey: Optional API key for authentication
    /// </summary>
    public static void ConfigureSerilogWithSeq(this WebApplicationBuilder builder)
    {
        // Read Seq configuration from IConfiguration (supports appsettings.json + environment variables)
        var seqServerUrl = builder.Configuration["Seq:ServerUrl"] 
                          ?? builder.Configuration["SEQ__SERVERURL"];
        var seqApiKey = builder.Configuration["Seq:ApiKey"] 
                       ?? builder.Configuration["SEQ__APIKEY"];

        // If Seq is configured, reconfigure Serilog with Seq sink
        if (!string.IsNullOrEmpty(seqServerUrl))
        {
            Log.Information("Configuring Seq logging: {SeqUrl}", seqServerUrl);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "ChatifyAI")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/chatai-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Seq(
                    serverUrl: seqServerUrl,
                    apiKey: seqApiKey,
                    batchPostingLimit: 100,
                    period: TimeSpan.FromSeconds(2))
                .CreateLogger();

            Log.Information("✅ Seq logging enabled: {SeqUrl}", seqServerUrl);
        }
        else
        {
            Log.Information("ℹ️  Seq not configured - using Console and File logging only");
        }
    }
}
