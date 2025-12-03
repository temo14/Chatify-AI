using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;

namespace ChatAI.Application.Interfaces;

public interface IChatService
{
    Task<ChatResponse> HandleAsync(ChatRequest request);
}
