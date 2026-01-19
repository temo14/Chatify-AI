namespace ChatAI.Domain.Interfaces.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data (tokens, secrets)
/// Uses ASP.NET Core Data Protection with envelope encryption pattern
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="keyVersion">Version of the encryption key (for key rotation)</param>
    /// <returns>Encrypted string (base64 encoded)</returns>
    string Encrypt(string plainText, int keyVersion = 1);
    
    /// <summary>
    /// Decrypts an encrypted string
    /// </summary>
    /// <param name="encryptedText">Encrypted text (base64 encoded)</param>
    /// <param name="keyVersion">Version of the encryption key used</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string encryptedText, int keyVersion = 1);
    
    /// <summary>
    /// Hashes a string using SHA256 (for verify tokens)
    /// </summary>
    /// <param name="text">Text to hash</param>
    /// <returns>SHA256 hash (hex string)</returns>
    string Hash(string text);
    
    /// <summary>
    /// Verifies a hash matches the plain text
    /// </summary>
    /// <param name="plainText">Plain text to verify</param>
    /// <param name="hash">Hash to compare against</param>
    /// <returns>True if hash matches</returns>
    bool VerifyHash(string plainText, string hash);
}
