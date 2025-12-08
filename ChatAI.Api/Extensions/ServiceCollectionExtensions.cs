using Azure;
using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.HealthChecks;
using ChatAI.Infrastructure.Repositories;
using ChatAI.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using AzureOpenAISDK = Azure.AI.OpenAI.AzureOpenAIClient;

namespace ChatAI.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add database configuration and Entity Framework services
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<ChatDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
            
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Add Azure OpenAI services (ChatClient, EmbeddingClient, Configuration)
    /// </summary>
    public static IServiceCollection AddAzureOpenAIServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind configuration options
        services.Configure<AzureOpenAIOptions>(
            configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<ChatOptions>(
            configuration.GetSection(ChatOptions.SectionName));
        services.Configure<QdrantOptions>(
            configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<ResilienceOptions>(
            configuration.GetSection("Resilience"));
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));
        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));

        // Register ChatClient for chat completions
        services.AddSingleton(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var azureClient = new AzureOpenAISDK(
                new Uri(config.Endpoint),
                new AzureKeyCredential(config.ApiKey));

            return azureClient.GetChatClient(config.ChatDeploymentName);
        });

        // Register EmbeddingClient for RAG embeddings
        services.AddSingleton(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var azureClient = new AzureOpenAISDK(
                new Uri(config.Endpoint),
                new AzureKeyCredential(config.ApiKey));

            return azureClient.GetEmbeddingClient(config.EmbeddingDeploymentName);
        });

        // Register Semantic Kernel (Scoped - to support scoped plugins)
        services.AddScoped(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var kernel = ChatAI.Infrastructure.AI.SemanticKernelFactory.CreateKernel(config, sp);
            
            // Add plugins with scoped dependencies
            kernel.Plugins.AddFromObject(
                sp.GetRequiredService<ChatAI.Application.Plugins.EmailPlugin>(),
                "EmailPlugin");
            
            return kernel;
        });

        return services;
    }

    /// <summary>
    /// Add application services (Semantic Kernel ChatService, Repositories, Cache)
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR for CQRS (Command/Query Responsibility Segregation)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SemanticKernelChatService).Assembly);
            
            // Add validation pipeline behavior
            cfg.AddOpenBehavior(typeof(ChatAI.Application.Behaviors.ValidationBehavior<,>));
        });

        // FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(SemanticKernelChatService).Assembly);
        
        // Memory cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 10000; // Max 10,000 cached items
        });
        
        // Cache service (Singleton - shared cache)
        services.AddSingleton<ICacheService, ChatAI.Infrastructure.Services.MemoryCacheService>();
        
        // Configuration service (Scoped - reads from database)
        services.AddScoped<IConfigurationService, ChatAI.Application.Services.ConfigurationService>();
        
        // Resilience policies (Singleton - shared policies)
        services.AddSingleton<ChatAI.Infrastructure.Resilience.ResiliencePolicies>();
        
        // Vector service (Singleton - shared connection pool)
        services.AddSingleton<IVectorService, QdrantVectorService>();
        
        // Email service (Singleton - shared SMTP client)
        services.AddSingleton<IEmailService, EmailService>();
        
        // Repositories (Scoped - per request lifecycle)
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

        // Chat context (Scoped - per request, tracks session info)
        services.AddScoped<ChatAI.Application.Services.ChatContext>();

        // Email plugin (Scoped - needs ChatContext which is scoped)
        services.AddScoped<ChatAI.Application.Plugins.EmailPlugin>();

        // Chat services - Using Semantic Kernel for AI orchestration
        services.AddScoped<IChatService, SemanticKernelChatService>();
        services.AddScoped<IChatStreamService, ChatStreamService>();

        return services;
    }

    /// <summary>
    /// Add health checks for external dependencies
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureConfig = configuration.GetSection(AzureOpenAIOptions.SectionName)
            .Get<AzureOpenAIOptions>() 
            ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is missing");

        var healthChecks = services.AddHealthChecks();

        // SQL Server health check
        healthChecks.AddSqlServer(
            connectionString,
            name: "sqlserver",
            tags: new[] { "database", "sql" });

        // Qdrant health check
        healthChecks.AddCheck<QdrantHealthCheck>(
            "qdrant",
            tags: new[] { "vector", "qdrant" });

        // Azure OpenAI health check
        healthChecks.AddCheck(
            "azureopenai",
            () =>
            {
                var azureClient = services.BuildServiceProvider()
                    .GetRequiredService<AzureOpenAISDK>();
                var logger = services.BuildServiceProvider()
                    .GetRequiredService<ILogger<AzureOpenAIHealthCheck>>();
                var check = new AzureOpenAIHealthCheck(azureClient, logger, azureConfig.ChatDeploymentName);
                return check.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext()).Result;
            },
            tags: new[] { "ai", "azureopenai" });

        return services;
    }

    /// <summary>
    /// Add Swagger/OpenAPI documentation with API Key authentication
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "Chatify AI API",
                Version = "v1",
                Description = "AI-powered chatbot with RAG, tool calling, and persistent conversations"
            });
        });
        
        return services;
    }
}
