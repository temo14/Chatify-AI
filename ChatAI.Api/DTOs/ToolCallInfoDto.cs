using ChatAI.Application.Models.Response;

namespace ChatAI.Api.DTOs;

public class ToolCallInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    public static ToolCallInfoDto FromDomain(ToolCallInfo toolCall)
    {
        return new ToolCallInfoDto
        {
            Name = toolCall.Name,
            Arguments = toolCall.Arguments,
            Result = toolCall.Result
        };
    }
}
