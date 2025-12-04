using Azure;
using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.Repositories;
using ChatAI.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using AzureOpenAISDK = Azure.AI.OpenAI.AzureOpenAIClient;
using ChatifyAIClient = ChatAI.Infrastructure.AI.AzureOpenAIClient;

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

        return services;
    }

    /// <summary>
    /// Add application services (ChatService, Repositories, Tools)
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repositories (Scoped - per request lifecycle)
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();

        // Application services
        services.AddScoped<IAIClient, ChatifyAIClient>();
        services.AddScoped<IChatService, ChatService>();
        
        // Tools (Singleton - shared across all requests)
        services.AddSingleton<IToolExecutor, ToolExecutor>();

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

            // Add API Key authentication to Swagger UI
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme 
            {
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Enter your API key (e.g., demo-key-12345, test-key-67890, or admin-key-abcdef)",
                Scheme = "ApiKeyScheme"
            });

            // Require API key for all endpoints
            c.AddSecurityRequirement(doc =>
            {
                var securityRequirement = new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("ApiKey", doc),
                        new List<string>()
                    }
                };
                return securityRequirement;
            });
        });
        
        return services;
    }
}
