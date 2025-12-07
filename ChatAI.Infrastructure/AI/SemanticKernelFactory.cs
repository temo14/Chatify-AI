using ChatAI.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace ChatAI.Infrastructure.AI;

/// <summary>
/// Factory for creating and configuring Semantic Kernel instance
/// Encapsulates SK setup in Infrastructure layer (proper DDD)
/// </summary>
public class SemanticKernelFactory
{
    public static Kernel CreateKernel(AzureOpenAIOptions options, IServiceProvider serviceProvider)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var kernelBuilder = Kernel.CreateBuilder();

        // Add service provider for plugin dependency injection
        kernelBuilder.Services.AddSingleton(serviceProvider);

        // Add Azure OpenAI chat completion connector
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: options.ChatDeploymentName,
            endpoint: options.Endpoint,
            apiKey: options.ApiKey);

        // Build kernel without plugins
        // Plugins will be added per-request in SemanticKernelChatService to respect scoped lifetime
        var kernel = kernelBuilder.Build();

        return kernel;
    }
}
