using System.Security.Cryptography;
using System.Text;
using ChatAI.Application.Interfaces;

namespace ChatAI.Application.Services;

/// <summary>
/// API key service implementation
/// </summary>
public class ApiKeyService : IApiKeyService
{
    public (string PlainKey, string KeyHash) GenerateApiKey()
    {
        // Generate a cryptographically secure random API key
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        
        var plainKey = $"chatai_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
        var keyHash = HashApiKey(plainKey);
        
        return (plainKey, keyHash);
    }
    
    public string HashApiKey(string plainKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(plainKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public string GenerateClientId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // First 16 characters of GUID
    }
}
