using ChatAI.Domain.Models.Request;
using ChatAI.Domain.Models.Response;

namespace ChatAI.Domain.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponse> HandleAsync(ChatRequest request);
}
