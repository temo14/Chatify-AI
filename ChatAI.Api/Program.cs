using ChatAI.Api.Extensions;
using ChatAI.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add custom services via extension methods
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddAzureOpenAIServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Auto-migrate and seed database (Development only)
await app.UseDatabaseMigrationsAsync(app.Environment);

// Configure middleware pipeline
app.UseHttpsRedirection();
app.UseSwaggerDocumentation(app.Environment);
app.UseApiKeyAuth();
app.UseAuthorization();
app.MapControllers();

app.Run();
