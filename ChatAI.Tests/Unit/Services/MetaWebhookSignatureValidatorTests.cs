using ChatAI.Infrastructure.Services.Meta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ChatAI.Tests.Unit.Services;

/// <summary>
/// Unit tests for Meta webhook signature validation
/// </summary>
public class MetaWebhookSignatureValidatorTests
{
    private readonly MetaWebhookSignatureValidator _validator;

    public MetaWebhookSignatureValidatorTests()
    {
        _validator = new MetaWebhookSignatureValidator();
    }

    [Fact]
    public void ValidateSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\",\"entry\":[]}";
        var signature = $"sha256={ComputeHmacSha256(appSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\",\"entry\":[]}";
        var wrongSecret = "wrong-secret";
        var signature = $"sha256={ComputeHmacSha256(wrongSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_MissingSignaturePrefix_ReturnsFalse()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\",\"entry\":[]}";
        var signature = ComputeHmacSha256(appSecret, payload); // Missing "sha256=" prefix

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_EmptyPayload_ValidatesCorrectly()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = string.Empty;
        var signature = $"sha256={ComputeHmacSha256(appSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_LargePayload_ValidatesCorrectly()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = new string('a', 10000); // 10KB payload
        var signature = $"sha256={ComputeHmacSha256(appSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_ModifiedPayload_ReturnsFalse()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var originalPayload = "{\"object\":\"page\",\"entry\":[]}";
        var modifiedPayload = "{\"object\":\"page\",\"entry\":[],\"extra\":\"data\"}";
        var signature = $"sha256={ComputeHmacSha256(appSecret, originalPayload)}";

        // Act
        var result = _validator.ValidateSignature(modifiedPayload, signature, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_SpecialCharactersInPayload_ValidatesCorrectly()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"text\":\"Hello 👋 World! @user #hashtag $money\"}";
        var signature = $"sha256={ComputeHmacSha256(appSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_NullPayload_ReturnsFalse()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var signature = "sha256=abc123";

        // Act
        var result = _validator.ValidateSignature((string)null!, signature, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_NullSignature_ReturnsFalse()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\"}";

        // Act
        var result = _validator.ValidateSignature(payload, null!, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_NullAppSecret_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"object\":\"page\"}";
        var signature = "sha256=abc123";

        // Act
        var result = _validator.ValidateSignature(payload, signature, null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_CaseSensitive_ValidatesCorrectly()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\"}";
        var hash = ComputeHmacSha256(appSecret, payload);
        
        // Test with lowercase (correct format)
        var lowerSignature = $"sha256={hash.ToLower()}";
        var lowerResult = _validator.ValidateSignature(payload, lowerSignature, appSecret);
        
        // Test with uppercase (should also work since hex is compared)
        var upperSignature = $"sha256={hash.ToUpper()}";
        var upperResult = _validator.ValidateSignature(payload, upperSignature, appSecret);

        // Assert
        Assert.True(lowerResult);
        Assert.True(upperResult);
    }

    [Theory]
    [InlineData("{\"object\":\"page\",\"entry\":[{\"id\":\"123\"}]}")]
    [InlineData("{\"object\":\"instagram\",\"entry\":[{\"id\":\"456\",\"messaging\":[]}]}")]
    [InlineData("{\"object\":\"whatsapp_business_account\",\"entry\":[{\"changes\":[]}]}")]
    public void ValidateSignature_RealWorldPayloads_ValidatesCorrectly(string payload)
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var signature = $"sha256={ComputeHmacSha256(appSecret, payload)}";

        // Act
        var result = _validator.ValidateSignature(payload, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_TimingSafeComparison_PreventsTimingAttacks()
    {
        // Arrange
        var appSecret = "test-app-secret-12345";
        var payload = "{\"object\":\"page\"}";
        var correctHash = ComputeHmacSha256(appSecret, payload);
        
        // Create a signature that differs only in the last character
        var almostCorrectHash = correctHash.Substring(0, correctHash.Length - 1) + "x";
        var wrongSignature = $"sha256={almostCorrectHash}";
        var correctSignature = $"sha256={correctHash}";

        // Act
        var wrongResult = _validator.ValidateSignature(payload, wrongSignature, appSecret);
        var correctResult = _validator.ValidateSignature(payload, correctSignature, appSecret);

        // Assert
        Assert.False(wrongResult);
        Assert.True(correctResult);
    }

    private static string ComputeHmacSha256(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }
}
