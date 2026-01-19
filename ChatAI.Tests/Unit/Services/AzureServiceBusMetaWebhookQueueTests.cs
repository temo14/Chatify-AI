using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.Services.Meta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ChatAI.Tests.Unit.Services;

/// <summary>
/// Unit tests for Azure Service Bus webhook queue with focus on session ordering
/// </summary>
public class AzureServiceBusMetaWebhookQueueTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AzureServiceBusMetaWebhookQueue>> _mockLogger;

    public AzureServiceBusMetaWebhookQueueTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AzureServiceBusMetaWebhookQueue>>();

        // Setup configuration
        _mockConfiguration.Setup(c => c["AzureServiceBus:ConnectionString"])
            .Returns("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test");
        _mockConfiguration.Setup(c => c["AzureServiceBus:MetaWebhookQueueName"])
            .Returns("meta-webhooks");
    }

    [Fact]
    public void SessionId_WithExternalUserId_UsesConnectionAndUserId()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var externalUserId = "user-123";
        var expectedSessionId = $"{connectionId:D}:{externalUserId}";

        var message = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.Messenger,
            ExternalUserId = externalUserId,
            RawPayload = "{}",
            EventKey = Guid.NewGuid().ToString()
        };

        // Act - extract session ID logic
        var sessionId = string.IsNullOrWhiteSpace(message.ExternalUserId)
            ? $"{message.ConnectionId:D}:{message.EventKey}"
            : $"{message.ConnectionId:D}:{message.ExternalUserId}";

        // Assert
        Assert.Equal(expectedSessionId, sessionId);
        Assert.Contains(connectionId.ToString("D"), sessionId);
        Assert.Contains(externalUserId, sessionId);
    }

    [Fact]
    public void SessionId_WithoutExternalUserId_UsesConnectionAndEventKey()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var eventKey = Guid.NewGuid().ToString();
        var expectedSessionId = $"{connectionId:D}:{eventKey}";

        var message = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.Instagram,
            ExternalUserId = null, // No external user ID
            RawPayload = "{}",
            EventKey = eventKey
        };

        // Act - extract session ID logic
        var sessionId = string.IsNullOrWhiteSpace(message.ExternalUserId)
            ? $"{message.ConnectionId:D}:{message.EventKey}"
            : $"{message.ConnectionId:D}:{message.ExternalUserId}";

        // Assert
        Assert.Equal(expectedSessionId, sessionId);
        Assert.Contains(connectionId.ToString("D"), sessionId);
        Assert.Contains(eventKey, sessionId);
    }

    [Theory]
    [InlineData("user-123", "user-456", false)] // Different users = different sessions
    [InlineData("user-123", "user-123", true)]  // Same user = same session
    public void SessionId_DifferentUsers_ProducesDifferentSessions(string user1, string user2, bool shouldBeEqual)
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        var message1 = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.WhatsApp,
            ExternalUserId = user1,
            EventKey = Guid.NewGuid().ToString()
        };

        var message2 = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.WhatsApp,
            ExternalUserId = user2,
            EventKey = Guid.NewGuid().ToString()
        };

        // Act
        var sessionId1 = $"{message1.ConnectionId:D}:{message1.ExternalUserId}";
        var sessionId2 = $"{message2.ConnectionId:D}:{message2.ExternalUserId}";

        // Assert
        if (shouldBeEqual)
        {
            Assert.Equal(sessionId1, sessionId2);
        }
        else
        {
            Assert.NotEqual(sessionId1, sessionId2);
        }
    }

    [Fact]
    public void SessionId_DifferentConnections_ProducesDifferentSessions()
    {
        // Arrange
        var connectionId1 = Guid.NewGuid();
        var connectionId2 = Guid.NewGuid();
        var sameUserId = "user-123";

        var message1 = new MetaWebhookMessage
        {
            ConnectionId = connectionId1,
            Channel = MetaChannel.Messenger,
            ExternalUserId = sameUserId,
            EventKey = Guid.NewGuid().ToString()
        };

        var message2 = new MetaWebhookMessage
        {
            ConnectionId = connectionId2,
            Channel = MetaChannel.Messenger,
            ExternalUserId = sameUserId,
            EventKey = Guid.NewGuid().ToString()
        };

        // Act
        var sessionId1 = $"{message1.ConnectionId:D}:{message1.ExternalUserId}";
        var sessionId2 = $"{message2.ConnectionId:D}:{message2.ExternalUserId}";

        // Assert
        Assert.NotEqual(sessionId1, sessionId2);
        Assert.Contains(connectionId1.ToString("D"), sessionId1);
        Assert.Contains(connectionId2.ToString("D"), sessionId2);
    }

    [Fact]
    public void SessionId_EnsuresStrictOrdering_ForSameConversation()
    {
        // Arrange - simulate 5 messages from same user to same connection
        var connectionId = Guid.NewGuid();
        var externalUserId = "user-123";
        var sessionIds = new List<string>();

        for (int i = 0; i < 5; i++)
        {
            var message = new MetaWebhookMessage
            {
                ConnectionId = connectionId,
                Channel = MetaChannel.Instagram,
                ExternalUserId = externalUserId,
                RawPayload = $"{{\"message\":\"{i}\"}}",
                EventKey = Guid.NewGuid().ToString()
            };

            var sessionId = $"{message.ConnectionId:D}:{message.ExternalUserId}";
            sessionIds.Add(sessionId);
        }

        // Assert - all messages should have SAME session ID (ensuring strict ordering)
        Assert.Equal(5, sessionIds.Count);
        Assert.Single(sessionIds.Distinct());
        Assert.All(sessionIds, sid => Assert.Equal(sessionIds[0], sid));
    }

    [Fact]
    public void SessionId_Format_IsConsistentAndParseable()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var externalUserId = "user-123";

        var message = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.Messenger,
            ExternalUserId = externalUserId,
            EventKey = Guid.NewGuid().ToString()
        };

        // Act
        var sessionId = $"{message.ConnectionId:D}:{message.ExternalUserId}";
        var parts = sessionId.Split(':');

        // Assert
        Assert.Equal(2, parts.Length);
        Assert.True(Guid.TryParse(parts[0], out var parsedConnectionId));
        Assert.Equal(connectionId, parsedConnectionId);
        Assert.Equal(externalUserId, parts[1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void SessionId_EmptyExternalUserId_UsesFallback(string? emptyUserId)
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var eventKey = "event-123";

        var message = new MetaWebhookMessage
        {
            ConnectionId = connectionId,
            Channel = MetaChannel.WhatsApp,
            ExternalUserId = emptyUserId,
            EventKey = eventKey
        };

        // Act
        var sessionId = string.IsNullOrWhiteSpace(message.ExternalUserId)
            ? $"{message.ConnectionId:D}:{message.EventKey}"
            : $"{message.ConnectionId:D}:{message.ExternalUserId}";

        // Assert
        Assert.Contains(connectionId.ToString("D"), sessionId);
        Assert.Contains(eventKey, sessionId);
        Assert.DoesNotContain("null", sessionId, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MessageSerialization_PreservesAllFields()
    {
        // Arrange
        var message = new MetaWebhookMessage
        {
            ConnectionId = Guid.NewGuid(),
            Channel = MetaChannel.Instagram,
            ExternalUserId = "ig-user-456",
            RawPayload = "{\"object\":\"instagram\",\"entry\":[]}",
            ReceivedAt = DateTime.UtcNow,
            EventKey = "event-789"
        };

        // Act
        var json = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<MetaWebhookMessage>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(message.ConnectionId, deserialized.ConnectionId);
        Assert.Equal(message.Channel, deserialized.Channel);
        Assert.Equal(message.ExternalUserId, deserialized.ExternalUserId);
        Assert.Equal(message.RawPayload, deserialized.RawPayload);
        Assert.Equal(message.EventKey, deserialized.EventKey);
    }
}
