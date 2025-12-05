using ChatAI.Application.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Factory for creating and configuring Semantic Kernel instance
/// Encapsulates SK setup in Infrastructure layer (proper DDD)
/// </summary>
public class SemanticKernelFactory
{
    public static Kernel CreateKernel(AzureOpenAIOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var kernelBuilder = Kernel.CreateBuilder();

        // Add Azure OpenAI chat completion connector
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: options.ChatDeploymentName,
            endpoint: options.Endpoint,
            apiKey: options.ApiKey);

        // Add plugins (Application layer business functions)
        kernelBuilder.Plugins.AddFromType<ChatAI.Application.Plugins.CalculatorPlugin>();
        kernelBuilder.Plugins.AddFromType<ChatAI.Application.Plugins.TimePlugin>();
        kernelBuilder.Plugins.AddFromType<ChatAI.Application.Plugins.TextUtilsPlugin>();

        return kernelBuilder.Build();
    }
}
