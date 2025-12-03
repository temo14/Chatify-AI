using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.AI;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Implementation of AI client for chat completions
/// Currently stubbed - ready for OpenAI/Azure OpenAI/Anthropic integration
/// </summary>
public class AIClient : IAIClient
{
    // TODO: Inject IConfiguration and IHttpClientFactory
    // TODO: Add support for OpenAI SDK, Azure OpenAI, or LM Studio
    
    public async Task<AIResponse> GenerateResponseAsync(List<ChatMessage> messages, bool allowTools = true)
    {
        // STUB IMPLEMENTATION
        // In production, this will call:
        // - OpenAI API
        // - Azure OpenAI
        // - LM Studio
        // - Anthropic Claude
        // - Or any other LLM provider
        
        await Task.Delay(100); // Simulate API call
        
        var lastUserMessage = messages
            .LastOrDefault(m => m.Role == MessageRole.User)?.Content 
            ?? "No message";
        
        return new AIResponse
        {
            Content = $"[AI Response] You said: {lastUserMessage}. This is a stub response. Integrate a real AI provider here.",
            ToolCall = null, // Set when AI wants to call a tool
            Model = "stub-model-v1",
            TokensUsed = 50,
            FinishReason = "stop"
        };
    }
}
