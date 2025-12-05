using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Service for streaming chat responses in real-time
/// </summary>
public interface IChatStreamService
{
    /// <summary>
    /// Handle chat request with streaming response
    /// </summary>
    /// <param name="request">Chat request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of stream chunks</returns>
    IAsyncEnumerable<StreamChunk> HandleStreamAsync(
        ChatRequest request, 
        CancellationToken cancellationToken = default);
}
