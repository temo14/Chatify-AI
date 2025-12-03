namespace ChatAI.Application.Models.Request;

public class ChatRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool UseTools { get; set; } = true;
}
