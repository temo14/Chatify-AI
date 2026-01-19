using Azure;
using ChatAI.Application.Configuration;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.HealthChecks;
using ChatAI.Infrastructure.Repositories;
using ChatAI.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
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
        // Skip SQL Server registration in test environment - tests will register InMemory
        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }
        
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
        // Qdrant disabled - using SQL vector storage only
        // services.Configure<QdrantOptions>(
        //     configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<ResilienceOptions>(
            configuration.GetSection("Resilience"));
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));
        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        // Register AzureOpenAISDK for health checks and other services
        services.AddSingleton(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            return new AzureOpenAISDK(
                new Uri(config.Endpoint),
                new AzureKeyCredential(config.ApiKey));
        });

        // Register ChatClient for chat completions
        services.AddSingleton(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var azureClient = sp.GetRequiredService<AzureOpenAISDK>();
            return azureClient.GetChatClient(config.ChatDeploymentName);
        });

        // Register EmbeddingClient for RAG embeddings
        services.AddSingleton(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var azureClient = sp.GetRequiredService<AzureOpenAISDK>();
            return azureClient.GetEmbeddingClient(config.EmbeddingDeploymentName);
        });

        // Register Semantic Kernel (Scoped - to support scoped plugins)
        services.AddScoped(sp =>
        {
            var config = configuration.GetSection(AzureOpenAIOptions.SectionName)
                .Get<AzureOpenAIOptions>() 
                ?? throw new InvalidOperationException("AzureOpenAI configuration is missing");

            var kernel = ChatAI.Infrastructure.AI.SemanticKernelFactory.CreateKernel(config, sp);
            
            // NOTE: Plugins are NOT registered here - they are registered per-request
            // in SemanticKernelChatService to respect tenant feature toggles
            // (EnableTools, EnableEmailSupport, etc.)
            
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
            cfg.RegisterServicesFromAssembly(typeof(ChatAI.Application.Behaviors.ValidationBehavior<,>).Assembly);
            
            // Add validation pipeline behavior
            cfg.AddOpenBehavior(typeof(ChatAI.Application.Behaviors.ValidationBehavior<,>));
        });

        // FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(ChatAI.Application.Behaviors.ValidationBehavior<,>).Assembly);
        
        // Memory cache (no SizeLimit to avoid conflicts with AspNetCoreRateLimit)
        services.AddMemoryCache();
        
        // Cache service (Singleton - shared cache)
        services.AddSingleton<ICacheService, ChatAI.Infrastructure.Services.MemoryCacheService>();
        
        // Configuration service (Scoped - reads from database)
        services.AddScoped<IConfigurationService, ChatAI.Infrastructure.Services.ConfigurationService>();
        
        // Resilience policies (Singleton - shared policies)
        services.AddSingleton<ChatAI.Infrastructure.Resilience.ResiliencePolicies>();
        
        // Email service (Singleton - shared SMTP client)
        services.AddSingleton<IEmailService, EmailService>();
        
        // Repositories (Scoped - per request lifecycle)
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IAdminUserRepository, ChatAI.Infrastructure.Repositories.AdminUserRepository>();
        services.AddScoped<IApiKeyRepository, ChatAI.Infrastructure.Repositories.ApiKeyRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>(); // Multi-tenancy support
        
        // Multi-tenancy services (Scoped - per request lifecycle)
        services.AddScoped<ITenantContext, ChatAI.Infrastructure.Services.TenantContext>();
        services.AddScoped<ChatAI.Infrastructure.Interfaces.IVectorStorageFactory, ChatAI.Infrastructure.Services.VectorStorageFactory>();
        
        // Authentication services
        services.AddScoped<IAuthService, ChatAI.Infrastructure.Services.AuthService>();
        services.AddSingleton<IApiKeyService, ChatAI.Infrastructure.Services.ApiKeyService>();

        // Chat context (Scoped - per request, tracks session info)
        services.AddScoped<ChatAI.Application.Services.ChatContext>();

        // Email plugin (Scoped - needs ChatContext which is scoped)
        services.AddScoped<ChatAI.Application.Plugins.EmailPlugin>();
        
        // Knowledge plugin (Scoped - needs ChatContext and KnowledgeRepository)
        services.AddScoped<ChatAI.Application.Plugins.KnowledgePlugin>();

        // Chat services - Using Semantic Kernel for AI orchestration
        services.AddScoped<IChatService, ChatAI.Infrastructure.Services.SemanticKernelChatService>();
        services.AddScoped<IChatStreamService, ChatAI.Infrastructure.Services.ChatStreamService>();

        return services;
    }
    
    /// <summary>
    /// Add Meta Channels integration services (Messenger, Instagram, WhatsApp)
    /// </summary>
    public static IServiceCollection AddMetaChannelsServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Encryption (Data Protection)
        // Keys persisted to SQL database to survive container restarts
        services.AddDataProtection()
            .PersistKeysToDbContext<ChatDbContext>()
            .SetApplicationName("ChatAI");
        
        services.AddScoped<IEncryptionService, EncryptionService>();
        
        // Repositories
        services.AddScoped<IMetaChannelConnectionRepository, ChatAI.Infrastructure.Repositories.MetaChannelConnectionRepository>();
        services.AddScoped<IMetaInboundDedupeRepository, ChatAI.Infrastructure.Repositories.MetaInboundDedupeRepository>();
        services.AddScoped<IMetaConversationMapRepository, ChatAI.Infrastructure.Repositories.MetaConversationMapRepository>();
        
        // Meta API clients
        services.AddHttpClient();
        services.AddScoped<IMetaMessengerClient, ChatAI.Infrastructure.Services.Meta.MetaMessengerClient>();
        services.AddScoped<IMetaInstagramClient, ChatAI.Infrastructure.Services.Meta.MetaInstagramClient>();
        services.AddScoped<IMetaWhatsAppClient, ChatAI.Infrastructure.Services.Meta.MetaWhatsAppClient>();
        services.AddScoped<IMetaTokenValidator, ChatAI.Infrastructure.Services.Meta.MetaTokenValidator>();
        services.AddScoped<ChatAI.Domain.Interfaces.Services.IMetaOAuthService, ChatAI.Infrastructure.Services.Meta.MetaOAuthService>();
        
        // Webhook infrastructure
        services.AddSingleton<ChatAI.Infrastructure.Services.Meta.IMetaWebhookSignatureValidator, ChatAI.Infrastructure.Services.Meta.MetaWebhookSignatureValidator>();
        
        // Webhook queue: Use Azure Service Bus in production, in-memory for development
        var serviceBusConnectionString = configuration["AzureServiceBus:ConnectionString"];
        var useServiceBus = !string.IsNullOrEmpty(serviceBusConnectionString);
        
        if (useServiceBus)
        {
            // Production: Azure Service Bus (durable, scalable)
            services.AddSingleton<IMetaWebhookQueue, ChatAI.Infrastructure.Services.Meta.AzureServiceBusMetaWebhookQueue>();
            services.AddHostedService<ChatAI.Infrastructure.Services.Meta.AzureServiceBusMetaWebhookProcessor>();
        }
        else
        {
            // Development: In-memory queue (simple, not durable)
            services.AddSingleton<IMetaWebhookQueue, ChatAI.Infrastructure.Services.Meta.InMemoryMetaWebhookQueue>();
            services.AddHostedService<ChatAI.Infrastructure.Services.Meta.MetaWebhookProcessorService>();
        }
        
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

        // Database migration status check
        healthChecks.AddCheck("database-migrations", () =>
        {
            try
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
                var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                
                if (pendingMigrations.Any())
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                        $"⚠️ {pendingMigrations.Count} pending migration(s): {string.Join(", ", pendingMigrations)}");
                }
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("✅ All migrations applied");
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "❌ Cannot check migrations", ex);
            }
        }, tags: new[] { "database", "migrations" });

        // Qdrant health check
        //healthChecks.AddCheck<QdrantHealthCheck>(
        //    "qdrant",
        //    tags: new[] { "vector", "qdrant" });

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

        // Azure Service Bus health check (if configured for production)
        var serviceBusConnectionString = configuration["AzureServiceBus:ConnectionString"];
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            var queueName = configuration["AzureServiceBus:MetaWebhookQueueName"]
                ?? configuration["AzureServiceBus:QueueName"]
                ?? "meta-webhooks";
            healthChecks.AddAzureServiceBusQueue(
                serviceBusConnectionString,
                queueName,
                name: "servicebus-meta-webhooks",
                tags: new[] { "messaging", "servicebus", "meta" });
        }

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
        services.AddAuthentication(options =>
        {
            // Default scheme for web/API
            options.DefaultAuthenticateScheme = "MultiScheme";
            options.DefaultChallengeScheme = "MultiScheme";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Configured via options below (deferred to allow test overrides)
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/admin-login.html";
            options.LogoutPath = "/api/auth/logout";
            // ExpireTimeSpan configured via options below
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
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                
                // Default to cookie for browser requests
                return CookieAuthenticationDefaults.AuthenticationScheme;
            };
        });

        // Configure JWT/Cookie options lazily using JwtOptions.
        // This avoids capturing an empty Jwt:Secret too early (important for tests and dynamic configuration).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptionsMonitor<JwtOptions>>((options, jwtOptionsMonitor) =>
            {
                var jwt = jwtOptionsMonitor.CurrentValue;

                if (string.IsNullOrWhiteSpace(jwt.Secret))
                {
                    throw new InvalidOperationException("JWT configuration is missing Jwt:Secret");
                }

                var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwt.Secret);

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptionsMonitor<JwtOptions>>((options, jwtOptionsMonitor) =>
            {
                var jwt = jwtOptionsMonitor.CurrentValue;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(jwt.ExpirationMinutes);
            });
        
        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // PlatformAdmin - Only Dott staff can manage customer tenants
            options.AddPolicy("PlatformAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("PlatformAdmin");
            });
            
            // TenantAdmin - Customer admins + Platform admins
            // Platform admins can access everything for support purposes
            options.AddPolicy("TenantAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("TenantAdmin", "PlatformAdmin");
            });
            
            // Client - End users authenticating via API key
            options.AddPolicy("Client", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Client");
            });
        });

        return services;
    }
}
