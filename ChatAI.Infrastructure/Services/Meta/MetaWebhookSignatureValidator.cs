using System.Security.Cryptography;
using System.Text;

namespace ChatAI.Infrastructure.Services.Meta;

/// <summary>
/// Service for validating Meta webhook signatures (X-Hub-Signature-256)
/// </summary>
public interface IMetaWebhookSignatureValidator
{
    /// <summary>
    /// Validate webhook signature
    /// </summary>
    /// <param name="payload">Raw request body bytes</param>
    /// <param name="signatureHeader">Value of X-Hub-Signature-256 header</param>
    /// <param name="appSecret">App secret for this connection</param>
    /// <returns>True if signature is valid</returns>
    bool ValidateSignature(byte[] payload, string signatureHeader, string appSecret);
    
    /// <summary>
    /// Validate webhook signature
    /// </summary>
    /// <param name="payload">Raw request body string</param>
    /// <param name="signatureHeader">Value of X-Hub-Signature-256 header</param>
    /// <param name="appSecret">App secret for this connection</param>
    /// <returns>True if signature is valid</returns>
    bool ValidateSignature(string payload, string signatureHeader, string appSecret);
}

public class MetaWebhookSignatureValidator : IMetaWebhookSignatureValidator
{
    public bool ValidateSignature(byte[] payload, string signatureHeader, string appSecret)
    {
        if (payload == null || string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(appSecret))
        {
            return false;
        }
        
        // Extract hash from header (format: "sha256=HASH")
        if (!signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        
        var receivedHash = signatureHeader.Substring(7); // Remove "sha256=" prefix
        
        // Compute expected hash
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHashBytes = hmac.ComputeHash(payload);
        var computedHash = BitConverter.ToString(computedHashBytes).Replace("-", "").ToLowerInvariant();
        
        // Log for debugging (will be visible in Seq)
        Console.WriteLine($"[SignatureValidator] Received: {receivedHash.ToLowerInvariant()}, Computed: {computedHash}");
        
        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(receivedHash.ToLowerInvariant())
        );
    }
    
    public bool ValidateSignature(string payload, string signatureHeader, string appSecret)
    {
        if (payload == null || string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(appSecret))
        {
            return false;
        }
        
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        return ValidateSignature(payloadBytes, signatureHeader, appSecret);
    }
}
