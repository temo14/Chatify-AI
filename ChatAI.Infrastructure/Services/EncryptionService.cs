using ChatAI.Domain.Interfaces.Services;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Implementation of encryption service using ASP.NET Core Data Protection
/// Provides encryption/decryption for sensitive data with key rotation support
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    
    public EncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
    }
    
    public string Encrypt(string plainText, int keyVersion = 1)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        
        // Create a protector with the key version
        var protector = _dataProtectionProvider.CreateProtector($"MetaChannels.v{keyVersion}");
        
        // Encrypt and return base64 encoded string
        return protector.Protect(plainText);
    }
    
    public string Decrypt(string encryptedText, int keyVersion = 1)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        
        // Create a protector with the same key version
        var protector = _dataProtectionProvider.CreateProtector($"MetaChannels.v{keyVersion}");
        
        // Decrypt and return plain text
        return protector.Unprotect(encryptedText);
    }
    
    public string Hash(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    public bool VerifyHash(string plainText, string hash)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(hash))
            return false;
        
        var computedHash = Hash(plainText);
        
        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash)
        );
    }
}
