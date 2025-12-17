using ChatAI.Application.Features.Session.GetSession;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ChatAI.Tests.Unit.Handlers;

public class GetSessionQueryHandlerTests
{
    private readonly Mock<IChatSessionRepository> _mockRepository;
    private readonly Mock<ILogger<GetSessionQueryHandler>> _mockLogger;
    private readonly GetSessionQueryHandler _handler;

    public GetSessionQueryHandlerTests()
    {
        _mockRepository = new Mock<IChatSessionRepository>();
        _mockLogger = new Mock<ILogger<GetSessionQueryHandler>>();
        _handler = new GetSessionQueryHandler(_mockRepository.Object, _mockLogger.Object);
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
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        var messages = new List<ChatMessage>
        {
            new ChatMessage { Id = Guid.NewGuid(), SessionId = sessionId, Role = MessageRole.User, Content = "Hello" },
            new ChatMessage { Id = Guid.NewGuid(), SessionId = sessionId, Role = MessageRole.Assistant, Content = "Hi!" },
            new ChatMessage { Id = Guid.NewGuid(), SessionId = sessionId, Role = MessageRole.User, Content = "How are you?" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);
        _mockRepository.Setup(r => r.GetSessionMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var query = new GetSessionQuery { SessionId = sessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Data);
        Assert.Equal(sessionId, result.Data.SessionId);
        Assert.Equal("user-456", result.Data.UserId);
        Assert.True(result.Data.IsActive);
        Assert.Equal(3, result.Data.MessageCount);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        // Arrange
        var sessionId = "nonexistent-session";
        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync((ChatSession?)null);

        var query = new GetSessionQuery { SessionId = sessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Session not found", result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Handle_InactiveSession_ReturnsCorrectStatus()
    {
        // Arrange
        var sessionId = "inactive-session";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = "user-789",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            LastActivityAt = DateTime.UtcNow.AddDays(-3),
            IsActive = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);
        _mockRepository.Setup(r => r.GetSessionMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        var query = new GetSessionQuery { SessionId = sessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.IsActive);
        Assert.Equal(0, result.Data.MessageCount);
    }
}
