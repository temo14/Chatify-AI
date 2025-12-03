namespace ChatAI.Application.Configuration;

/// <summary>
/// Configuration for Azure OpenAI service
/// Maps to appsettings.json
/// </summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = "gpt-4";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
}
