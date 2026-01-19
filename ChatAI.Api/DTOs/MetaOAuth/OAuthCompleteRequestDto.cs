namespace ChatAI.Api.DTOs.MetaOAuth;

/// <summary>
/// Authenticated OAuth completion request.
/// The UI calls this after Meta redirects back to the UI with code/state.
/// </summary>
public sealed class OAuthCompleteRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
