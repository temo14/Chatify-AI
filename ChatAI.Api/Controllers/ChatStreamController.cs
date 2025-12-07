using ChatAI.Api.DTOs;
using ChatAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Controller for streaming chat responses using Server-Sent Events (SSE)
/// Thin controller - handles HTTP streaming concerns, delegates logic to ChatStreamService
/// </summary>
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
        _chatStreamService = chatStreamService ?? throw new ArgumentNullException(nameof(chatStreamService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Stream chat response using Server-Sent Events (SSE)
    /// Returns real-time AI response chunks as they're generated
    /// </summary>
    /// <param name="dto">Chat request with message and optional sessionId</param>
    /// <param name="cancellationToken">Cancellation token for client disconnect</param>
    /// <response code="200">Streaming response initiated</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task StreamMessage(
        [FromBody] ChatRequestDto dto, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new { error = "Invalid request" }, cancellationToken);
            return;
        }

        // Configure SSE headers
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering
        
        var request = dto.ToDomain();
        _logger.LogInformation("Streaming chat request - UserId: {UserId}, SessionId: {SessionId}", 
            request.UserId ?? "anonymous", request.SessionId ?? "new");

        try
        {
            await foreach (var chunk in _chatStreamService.HandleStreamAsync(request, cancellationToken))
            {
                var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var sseData = $"data: {json}\n\n";
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Client disconnected from stream");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming response");
            
            var errorJson = JsonSerializer.Serialize(new 
            { 
                error = "An error occurred while streaming the response",
                isComplete = true 
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            
            await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
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
