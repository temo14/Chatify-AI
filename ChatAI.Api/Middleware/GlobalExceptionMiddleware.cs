using System.Net;
using System.Text.Json;
using ChatAI.Application.Exceptions;

namespace ChatAI.Api.Middleware;

/// <summary>
/// Global exception handler middleware - catches all unhandled exceptions
/// Provides consistent error responses and prevents leaking sensitive information
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        string message;
        string errorCode;

        switch (exception)
        {
            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                message = notFound.Message;
                errorCode = "NOT_FOUND";
                break;
            case FluentValidation.ValidationException validation:
                statusCode = HttpStatusCode.BadRequest;
                message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                errorCode = "VALIDATION_ERROR";
                break;
            case AIServiceException aiError:
                statusCode = HttpStatusCode.ServiceUnavailable;
                message = "AI service is temporarily unavailable";
                errorCode = "AI_ERROR";
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Unauthorized access";
                errorCode = "UNAUTHORIZED";
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred";
                errorCode = "INTERNAL_ERROR";
                break;
        }

        // Log the exception with full details
        _logger.LogError(exception, 
            "Unhandled exception: {ErrorCode} - {Message} | Path: {Path}", 
            errorCode, 
            exception.Message,
            context.Request.Path);

        // Build error response
        var response = new
        {
            error = new
            {
                code = errorCode,
                message = message,
                // Only include details in Development
                details = _environment.IsDevelopment() ? exception.Message : null,
                stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                path = context.Request.Path.Value,
                timestamp = DateTime.UtcNow
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
