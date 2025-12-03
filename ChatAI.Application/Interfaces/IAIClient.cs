using ChatAI.Application.Models.AI;
using ChatAI.Domain.Entities;

namespace ChatAI.Application.Interfaces;

/// <summary>
/// Interface for AI provider integration
/// </summary>
public interface IAIClient
{
    /// <summary>
    /// Generates a response from the AI model
    /// </summary>
    /// <param name="messages">Conversation history</param>
    /// <param name="allowTools">Whether to enable tool/function calling</param>
    /// <returns>AI response with content and optional tool calls</returns>
    Task<AIResponse> GenerateResponseAsync(List<ChatMessage> messages, bool allowTools = true);
}
