using ChatAI.Application.Interfaces;
using ChatAI.Application.Services;
using ChatAI.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAIClient, AIClient>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
