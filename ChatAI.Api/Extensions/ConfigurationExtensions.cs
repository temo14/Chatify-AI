using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace ChatAI.Api.Extensions;

/// <summary>
/// Extension methods for configuring application configuration sources (Key Vault, etc.)
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures Azure Key Vault as a configuration source for production environments.
    /// 
    /// DefaultAzureCredential authentication flow:
    /// 1. Environment Variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
    /// 2. Managed Identity (recommended for Azure App Service, AKS, VMs)
    /// 3. Visual Studio credentials (local development)
    /// 4. Azure CLI credentials (local development)
    /// 5. Azure PowerShell credentials (local development)
    /// 
    /// For production, use Managed Identity (no credentials needed in code).
    /// 
    /// Note: This method intentionally does NOT catch exceptions. If Key Vault configuration
    /// fails, the exception will bubble up to Program.cs where it's handled appropriately.
    /// This follows the fail-fast principle - if Key Vault is configured but inaccessible,
    /// the application should not start with potentially missing critical secrets.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <param name="logger">Logger for configuration messages</param>
    /// <exception cref="InvalidOperationException">Thrown when Key Vault endpoint is invalid</exception>
    /// <exception cref="Azure.RequestFailedException">Thrown when Key Vault is inaccessible</exception>
    public static void AddAzureKeyVaultConfiguration(
        this WebApplicationBuilder builder,
        ILogger? logger = null)
    {
        // Only configure Key Vault in Production
        if (!builder.Environment.IsProduction())
        {
            logger?.LogInformation("Non-production environment - skipping Key Vault configuration");
            return;
        }

        var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];

        if (string.IsNullOrEmpty(keyVaultEndpoint))
        {
            logger?.LogWarning("⚠️ KeyVault:Endpoint not configured. Using environment variables only.");
            return;
        }

        // Validate endpoint format early
        if (!Uri.TryCreate(keyVaultEndpoint, UriKind.Absolute, out var vaultUri))
        {
            throw new InvalidOperationException(
                $"Invalid KeyVault:Endpoint format: '{keyVaultEndpoint}'. " +
                "Expected format: https://your-keyvault.vault.azure.net/");
        }

        logger?.LogInformation("Configuring Azure Key Vault: {Endpoint}", keyVaultEndpoint);

        // DefaultAzureCredential will automatically use:
        // - Managed Identity in Azure App Service (production)
        // - Azure CLI credentials for local testing
        // - Visual Studio credentials for local development
        var credential = new DefaultAzureCredential();

        // Add Key Vault to configuration - let exceptions bubble up if it fails
        // This is intentional: if Key Vault is configured but fails, we want to know immediately
        builder.Configuration.AddAzureKeyVault(vaultUri, credential);

        logger?.LogInformation("✅ Azure Key Vault configured successfully");
        logger?.LogInformation("   Authentication: DefaultAzureCredential (Managed Identity or local dev credentials)");
    }
}
