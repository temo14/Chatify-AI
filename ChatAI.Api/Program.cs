using ChatAI.Api.Extensions;
using ChatAI.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddAzureOpenAIServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

await app.UseDatabaseMigrationsAsync(app.Environment);

app.UseHttpsRedirection();
app.UseSwaggerDocumentation(app.Environment);
app.UseApiKeyAuth();
app.UseAuthorization();
app.MapControllers();

app.Run();
