using ChatAI.Api.DTOs;
using ChatAI.Application.Commands;
using ChatAI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ChatAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatStreamController : ControllerBase
{
    private readonly IChatStreamService _chatStreamService;
    private readonly ILogger<ChatStreamController> _logger;

    public ChatStreamController(
        IChatStreamService chatStreamService, 
        ILogger<ChatStreamController> logger)
    {
        _chatStreamService = chatStreamService;
        _logger = logger;
    }

    /// <summary>
    /// Stream chat response using Server-Sent Events (SSE)
    /// Note: Streaming uses IChatStreamService directly as MediatR doesn't natively support IAsyncEnumerable
    /// </summary>
    [HttpPost("stream")]
    public async Task<IActionResult> StreamMessage(
        [FromBody] ChatRequestDto dto, 
        CancellationToken cancellationToken)
    {
        // Get authenticated user ID
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? dto.UserId;
        
        _logger.LogInformation("Streaming chat request from user: {UserId}", userId);
        
        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering
        
        var request = dto.ToDomain();
        request.UserId = userId;

        try
        {
            await foreach (var chunk in _chatStreamService.HandleStreamAsync(request, cancellationToken))
            {
                // Serialize chunk to JSON
                var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                // Write SSE format: data: {...}\n\n
                var sseData = $"data: {json}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseData);
                
                await Response.Body.WriteAsync(bytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // Check if client disconnected
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Client disconnected from stream");
                    break;
                }
            }
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming response");
            
            // Send error event
            var errorJson = JsonSerializer.Serialize(new 
            { 
                error = "An error occurred while streaming the response",
                isComplete = true 
            });
            var errorData = $"data: {errorJson}\n\n";
            var errorBytes = Encoding.UTF8.GetBytes(errorData);
            
            await Response.Body.WriteAsync(errorBytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Example endpoint showing how to consume the stream from JavaScript
    /// </summary>
    [HttpGet("stream/example")]
    public IActionResult GetStreamExample()
    {
        var example = @"
// JavaScript example for consuming the streaming endpoint

const eventSource = new EventSource('/api/chatstream/stream', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-API-Key': 'your-api-key'
    },
    body: JSON.stringify({
        userId: 'user123',
        message: 'Hello!',
        sessionId: null
    })
});

eventSource.onmessage = (event) => {
    const chunk = JSON.parse(event.data);
    
    if (chunk.error) {
        console.error('Error:', chunk.error);
        eventSource.close();
        return;
    }
    
    if (chunk.isComplete) {
        console.log('Stream complete!');
        eventSource.close();
    } else {
        // Append chunk to UI
        document.getElementById('response').innerText += chunk.content;
    }
};

eventSource.onerror = (error) => {
    console.error('SSE Error:', error);
    eventSource.close();
};

// Or use fetch with ReadableStream for more control:
async function streamChat(message) {
    const response = await fetch('/api/chatstream/stream', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': 'your-api-key'
        },
        body: JSON.stringify({
            userId: 'user123',
            message: message,
            sessionId: null
        })
    });

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value);
        const lines = chunk.split('\n\n');
        
        for (const line of lines) {
            if (line.startsWith('data: ')) {
                const data = JSON.parse(line.substring(6));
                console.log('Chunk:', data);
                
                if (!data.isComplete) {
                    // Append to UI
                    document.getElementById('response').innerText += data.content;
                }
            }
        }
    }
}
";

        return Content(example, "text/plain");
    }
}
