using ChatAI.Application.Handlers;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Queries;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatAI.Tests.Unit.Handlers;

public class ExportSessionQueryHandlerTests
{
    private readonly Mock<IChatSessionRepository> _mockRepository;
    private readonly Mock<ILogger<ExportSessionQueryHandler>> _mockLogger;
    private readonly ExportSessionQueryHandler _handler;

    public ExportSessionQueryHandlerTests()
    {
        _mockRepository = new Mock<IChatSessionRepository>();
        _mockLogger = new Mock<ILogger<ExportSessionQueryHandler>>();
        _handler = new ExportSessionQueryHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidSession_ReturnsSuccessWithData()
    {
        // Arrange
        var sessionId = "session-123";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = "user-456",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.User,
                Content = "Hello",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.Assistant,
                Content = "Hi there!",
                Timestamp = DateTime.UtcNow
            }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);
        _mockRepository.Setup(r => r.GetSessionMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var query = new ExportSessionQuery
        {
            SessionId = sessionId,
            Format = ExportFormat.Json
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Data);
        Assert.Equal(sessionId, result.Data.SessionId);
        Assert.Equal("user-456", result.Data.UserId);
        Assert.Equal(2, result.Data.MessageCount);
        Assert.Equal(2, result.Data.Messages.Count);
        Assert.Equal("User", result.Data.Messages[0].Role);
        Assert.Equal("Hello", result.Data.Messages[0].Content);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync((ChatSession?)null);

        var query = new ExportSessionQuery
        {
            SessionId = sessionId,
            Format = ExportFormat.Csv
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Session not found", result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_EmptySession_ReturnsSuccessWithZeroMessages()
    {
        // Arrange
        var sessionId = "empty-session";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = "user-789",
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);
        _mockRepository.Setup(r => r.GetSessionMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        var query = new ExportSessionQuery { SessionId = sessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.MessageCount);
        Assert.Empty(result.Data.Messages);
    }
}
