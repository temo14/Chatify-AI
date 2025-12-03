using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Request;
using ChatAI.Application.Models.Response;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;

namespace ChatAI.Application.Services;

/// <summary>
/// Main orchestrator for chat interactions
/// </summary>
public class ChatService : IChatService
{
    private readonly IAIClient _ai;

    public ChatService(IAIClient ai)
    {
        _ai = ai;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest request)
    {
        // STEP 1: Build conversation history
        var messages = BuildConversationHistory(request);

        // STEP 2: FIRST AI CALL - Ask for response
        var aiResponse = await _ai.GenerateResponseAsync(messages, request.UseTools);

        // STEP 3: If AI wants to use a tool, execute it and call AI AGAIN
        if (aiResponse.ToolCall != null)
        {
            // Execute the actual tool (YOUR code runs here)
            var toolResult = await ExecuteToolAsync(aiResponse.ToolCall.Name, aiResponse.ToolCall.Arguments);
            
            // Add assistant's tool call request to conversation
            messages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = request.UserId,
                Role = MessageRole.Assistant,
                Content = aiResponse.Content ?? string.Empty,
                IsToolCall = true,
                ToolName = aiResponse.ToolCall.Name,
                ToolArguments = aiResponse.ToolCall.Arguments,
                Timestamp = DateTime.UtcNow
            });
            
            // Add tool result to conversation
            messages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = request.UserId,
                Role = MessageRole.Tool,
                Content = toolResult,
                ToolName = aiResponse.ToolCall.Name,
                ToolResult = toolResult,
                Timestamp = DateTime.UtcNow
            });
            
            // STEP 4: SECOND AI CALL - Get final answer using tool result
            var finalResponse = await _ai.GenerateResponseAsync(messages, allowTools: false);
            
            return new ChatResponse
            {
                Reply = finalResponse.Content,  // ← Final answer using tool data
                SessionId = request.SessionId,
                ToolCalled = true,
                ToolCall = new ToolCallInfo
                {
                    Name = aiResponse.ToolCall.Name,
                    Arguments = aiResponse.ToolCall.Arguments,
                    Result = toolResult
                },
                ContextUsed = messages
            };
        }

        // STEP 5: No tool needed, return direct response
        return new ChatResponse
        {
            Reply = aiResponse.Content,
            SessionId = request.SessionId,
            ToolCalled = false,
            ToolCall = null,
            ContextUsed = messages
        };
    }

    /// <summary>
    /// Builds conversation history for AI context
    /// TODO: Load from database/session store
    /// </summary>
    private List<ChatMessage> BuildConversationHistory(ChatRequest request)
    {
        // For now, just create a single user message
        // Later: Load from database + add system prompt + user memory
        
        var messages = new List<ChatMessage>
        {
            // System message (optional)
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = request.UserId,
                Role = MessageRole.System,
                Content = "You are a helpful AI assistant.",
                Timestamp = DateTime.UtcNow
            },
            
            // User's current message
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = request.UserId,
                Role = MessageRole.User,
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            }
        };

        return messages;
    }

    /// <summary>
    /// Executes a tool call
    /// TODO: Implement proper tool registry and execution
    /// </summary>
    private async Task<string> ExecuteToolAsync(string toolName, string arguments)
    {
        // STUB: Tool execution logic
        await Task.Delay(50);
        return $"Tool '{toolName}' executed with args: {arguments}";
    }
}
