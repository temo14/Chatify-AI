using ChatAI.Application.Configuration;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models;
using ChatAI.Domain.Models.Request;
using ChatAI.Domain.Models.Response;
using ChatAI.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatAI.Tests.Unit.Services;

public class ChatStreamServiceTests
{
    private readonly Kernel _kernel;
    private readonly Mock<IChatSessionRepository> _mockSessionRepository;
    private readonly Mock<IKnowledgeRepository> _mockKnowledgeRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ILogger<ChatStreamService>> _mockLogger;
    private readonly Mock<IOptions<ChatOptions>> _mockChatOptions;
    private readonly Mock<IOptions<CacheOptions>> _mockCacheOptions;
    private readonly Mock<IChatCompletionService> _mockChatCompletion;
    
    private readonly ChatStreamService _sut;

    public ChatStreamServiceTests()
    {
        // Create a real Kernel instance with mocked chat completion service
        _mockChatCompletion = new Mock<IChatCompletionService>();
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(_mockChatCompletion.Object);
        _kernel = kernelBuilder.Build();

        _mockSessionRepository = new Mock<IChatSessionRepository>();
        _mockKnowledgeRepository = new Mock<IKnowledgeRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockLogger = new Mock<ILogger<ChatStreamService>>();

        var chatOptions = new ChatOptions
        {
            MaxConversationHistory = 10,
            RagTopK = 3
        };
        _mockChatOptions = new Mock<IOptions<ChatOptions>>();
        _mockChatOptions.Setup(x => x.Value).Returns(chatOptions);

        var cacheOptions = new CacheOptions
        {
            ConversationExpirationMinutes = 60
        };
        _mockCacheOptions = new Mock<IOptions<CacheOptions>>();
        _mockCacheOptions.Setup(x => x.Value).Returns(cacheOptions);

        // Mock tenant context and repository
        var tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(x => x.RequiredTenantId).Returns(tenantId);
        
        var tenant = new Tenant
        {
            Id = tenantId,
            Settings = new TenantSettings
            {
                SystemPrompt = "Test AI assistant",
                Temperature = 0.7,
                MaxTokens = 1500
            }
        };
        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _sut = new ChatStreamService(
            _kernel,
            _mockSessionRepository.Object,
            _mockKnowledgeRepository.Object,
            _mockCacheService.Object,
            _mockTenantContext.Object,
            _mockTenantRepository.Object,
            _mockLogger.Object,
            _mockChatOptions.Object,
            _mockCacheOptions.Object
        );
    }

    [Fact]
    public async Task HandleStreamAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in _sut.HandleStreamAsync(null!))
            {
                // Should throw before entering loop
            }
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleStreamAsync_WithEmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "",
            UserId = "user1"
        };

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in _sut.HandleStreamAsync(request))
            {
                // Should throw before entering loop
            }
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Message cannot be empty*");
    }

    [Fact]
    public async Task HandleStreamAsync_WithWhitespaceMessage_ThrowsArgumentException()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "   ",
            UserId = "user1"
        };

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in _sut.HandleStreamAsync(request))
            {
                // Should throw
            }
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleStreamAsync_CreatesNewSession_WhenSessionIdIsNull()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "Hello",
            UserId = "user1",
            SessionId = null
        };

        var newSession = new ChatSession
        {
            Id = "new-session-123",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _mockSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync(newSession);

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeDocument>());

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "Test response")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var chunks = new List<StreamChunk>();
        await foreach (var chunk in _sut.HandleStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        _mockSessionRepository.Verify(x => x.AddAsync(It.Is<ChatSession>(s =>
            s.UserId == "user1" &&
            s.IsActive == true
        )), Times.Once);

        chunks.Should().NotBeEmpty();
        chunks.Last().SessionId.Should().Be(newSession.Id);
        chunks.Last().IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStreamAsync_UsesExistingSession_WhenSessionIdProvided()
    {
        // Arrange
        var existingSession = new ChatSession
        {
            Id = "existing-123",
            UserId = "user1",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };

        var request = new ChatRequest
        {
            Message = "Hello",
            UserId = "user1",
            SessionId = existingSession.Id
        };

        _mockSessionRepository.Setup(x => x.GetByIdAsync(existingSession.Id))
            .ReturnsAsync(existingSession);

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeDocument>());

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "Response")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var chunks = new List<StreamChunk>();
        await foreach (var chunk in _sut.HandleStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        _mockSessionRepository.Verify(x => x.GetByIdAsync(existingSession.Id), Times.Once);
        _mockSessionRepository.Verify(x => x.AddAsync(It.IsAny<ChatSession>()), Times.Never);
        chunks.Last().SessionId.Should().Be(existingSession.Id);
    }

    [Fact]
    public async Task HandleStreamAsync_PerformsRAGSearch_WithUserMessage()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "How do I reset my password?",
            UserId = "user1"
        };

        var session = new ChatSession { Id = "session-123", UserId = "user1" };
        _mockSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync(session);

        var knowledgeDocs = new List<KnowledgeDocument>
        {
            new KnowledgeDocument
            {
                Title = "Password Reset Guide",
                Content = "To reset password, go to settings..."
            }
        };

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(request.Message, 3))
            .ReturnsAsync(knowledgeDocs);

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "Response")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        await foreach (var _ in _sut.HandleStreamAsync(request))
        {
            // Consume stream
        }

        // Assert
        _mockKnowledgeRepository.Verify(x => x.SearchAsync(request.Message, 3), Times.Once);
    }

    [Fact]
    public async Task HandleStreamAsync_SavesUserAndAssistantMessages_AfterStreaming()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "Test message",
            UserId = "user1"
        };

        var session = new ChatSession { Id = "session-123", UserId = "user1" };
        _mockSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync(session);

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeDocument>());

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "AI response")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        await foreach (var _ in _sut.HandleStreamAsync(request))
        {
            // Consume stream
        }

        // Assert
        _mockSessionRepository.Verify(x => x.AddMessagesAsync(
            It.Is<List<ChatMessage>>(messages =>
                messages.Count == 2 &&
                messages[0].Role == MessageRole.User &&
                messages[0].Content == "Test message" &&
                messages[1].Role == MessageRole.Assistant &&
                messages[1].Content == "AI response"
            )), Times.Once);
    }

    [Fact]
    public async Task HandleStreamAsync_ReturnsStreamChunks_WithSequenceNumbers()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "Test",
            UserId = "user1"
        };

        var session = new ChatSession { Id = "session-123", UserId = "user1" };
        _mockSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync(session);

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeDocument>());

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "Part1"),
            new StreamingChatMessageContent(AuthorRole.Assistant, "Part2"),
            new StreamingChatMessageContent(AuthorRole.Assistant, "Part3")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        var chunks = new List<StreamChunk>();
        await foreach (var chunk in _sut.HandleStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().HaveCount(4); // 3 content + 1 complete
        chunks[0].SequenceNumber.Should().Be(1);
        chunks[1].SequenceNumber.Should().Be(2);
        chunks[2].SequenceNumber.Should().Be(3);
        chunks[3].SequenceNumber.Should().Be(4);
        chunks[3].IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStreamAsync_InvalidatesCache_AfterSavingMessages()
    {
        // Arrange
        var request = new ChatRequest
        {
            Message = "Test",
            UserId = "user1"
        };

        var session = new ChatSession { Id = "session-123", UserId = "user1" };
        _mockSessionRepository.Setup(x => x.AddAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync(session);

        _mockKnowledgeRepository.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeDocument>());

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ChatMessage>>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<ChatMessage>());

        var mockStreamingContent = new List<StreamingChatMessageContent>
        {
            new StreamingChatMessageContent(AuthorRole.Assistant, "Response")
        };

        _mockChatCompletion.Setup(x => x.GetStreamingChatMessageContentsAsync(
                It.IsAny<Microsoft.SemanticKernel.ChatCompletion.ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockStreamingContent.ToAsyncEnumerable());

        _mockSessionRepository.Setup(x => x.AddMessagesAsync(It.IsAny<List<ChatMessage>>()))
            .Returns(Task.CompletedTask);

        _mockSessionRepository.Setup(x => x.UpdateAsync(It.IsAny<ChatSession>()))
            .Returns(Task.CompletedTask);

        // Act
        await foreach (var _ in _sut.HandleStreamAsync(request))
        {
            // Consume stream
        }

        // Assert
        _mockCacheService.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
    }
}

// Extension method to convert IEnumerable to IAsyncEnumerable for testing
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
