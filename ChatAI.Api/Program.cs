using Azure;
using ChatAI.Api.Middleware;
using ChatAI.Application.Configuration;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Infrastructure.Data;
using ChatAI.Infrastructure.Repositories;
using ChatAI.Infrastructure.Tools;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using OpenAI.Embeddings;
using AzureOpenAISDK = Azure.AI.OpenAI.AzureOpenAIClient;
using ChatifyAIClient = ChatAI.Infrastructure.AI.AzureOpenAIClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure options from appsettings.json
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection(AzureOpenAIOptions.SectionName));
builder.Services.Configure<ChatOptions>(
    builder.Configuration.GetSection(ChatOptions.SectionName));

// Register Database (Entity Framework Core)
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
    // Enable for development debugging
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Register Azure OpenAI clients
builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>();
    var azureClient = new AzureOpenAISDK(
        new Uri(config!.Endpoint),
        new AzureKeyCredential(config.ApiKey));
    
    return azureClient.GetChatClient(config.ChatDeploymentName);
});

builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>();
    var azureClient = new AzureOpenAISDK(
        new Uri(config!.Endpoint),
        new AzureKeyCredential(config.ApiKey));
    
    return azureClient.GetEmbeddingClient(config.EmbeddingDeploymentName);
});

// Register repositories (Scoped - per request lifecycle)
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();

// Register application services
builder.Services.AddScoped<IAIClient, ChatifyAIClient>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IToolExecutor, ToolExecutor>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate and seed database on startup (only in development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Running database migrations...");
    await db.Database.MigrateAsync();
    
    logger.LogInformation("Seeding database with test data...");
    var seeder = new DbSeeder(db, scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>());
    await seeder.SeedAsync();
    
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable API Key authentication
app.UseApiKeyAuth();

app.UseAuthorization();

app.MapControllers();

app.Run();
