using Azure;
using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.HealthChecks;
using ChatAI.Infrastructure.Repositories;
using ChatAI.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

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
        
        // Memory cache (no SizeLimit to avoid conflicts with AspNetCoreRateLimit)
        services.AddMemoryCache();
        
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
        services.AddScoped<IAdminUserRepository, ChatAI.Infrastructure.Repositories.AdminUserRepository>();
        services.AddScoped<IApiKeyRepository, ChatAI.Infrastructure.Repositories.ApiKeyRepository>();
        
        // Authentication services
        services.AddScoped<IAuthService, ChatAI.Application.Services.AuthService>();
        services.AddSingleton<IApiKeyService, ChatAI.Application.Services.ApiKeyService>();

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
    
    /// <summary>
    /// Add authentication and authorization services (JWT + API Key + Cookie)
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() 
            ?? throw new InvalidOperationException("JWT configuration is missing");
        
        var key = System.Text.Encoding.UTF8.GetBytes(jwtConfig.Secret);
        
        services.AddAuthentication(options =>
        {
            // Default scheme for web/API
            options.DefaultAuthenticateScheme = "MultiScheme";
            options.DefaultChallengeScheme = "MultiScheme";
        })
        .AddJwtBearer("JwtBearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddCookie("Cookie", options =>
        {
            options.LoginPath = "/admin-login.html";
            options.LogoutPath = "/api/auth/logout";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(jwtConfig.ExpirationMinutes);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.Name = "ChatifyAI.Auth";
        })
        .AddScheme<AuthenticationSchemeOptions, ChatAI.Infrastructure.Services.ApiKeyAuthenticationHandler>("ApiKey", null)
        .AddPolicyScheme("MultiScheme", "Multi-scheme authentication", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // Check for API key first
                if (context.Request.Headers.ContainsKey("X-API-Key"))
                {
                    return "ApiKey";
                }
                
                // Check for JWT token
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return "JwtBearer";
                }
                
                // Default to cookie for browser requests
                return "Cookie";
            };
        });
        
        // Add authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });
            
            options.AddPolicy("Client", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Client");
            });
            
            options.AddPolicy("AdminOrClient", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "Client");
            });
        });

        return services;
    }
}
