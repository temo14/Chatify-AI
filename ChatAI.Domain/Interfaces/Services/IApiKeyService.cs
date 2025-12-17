using ChatAI.Domain.Entities;

namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Service for API key management
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generate a new API key (returns plain key and hash)
    /// </summary>
    (string PlainKey, string KeyHash) GenerateApiKey();
    
    /// <summary>
    /// Hash an API key using SHA256
    /// </summary>
    string HashApiKey(string plainKey);
    
    /// <summary>
    /// Generate a unique client ID
    /// </summary>
    string GenerateClientId();
}
