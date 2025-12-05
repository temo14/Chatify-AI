# ChatAI Tests

Comprehensive test suite for Chatify AI covering unit tests, integration tests, and end-to-end scenarios.

## Test Structure

```
ChatAI.Tests/
├── Unit/                           # Fast, isolated unit tests
│   ├── QdrantVectorServiceTests.cs   # Vector service tests
│   └── KnowledgeRepositoryTests.cs   # RAG repository tests
├── Integration/                    # Tests requiring external services
│   └── VectorSearchIntegrationTests.cs  # Qdrant integration tests
└── GlobalUsings.cs                 # Shared test imports
```

## Running Tests

### All Tests
```powershell
dotnet test
```

### Unit Tests Only (fast)
```powershell
dotnet test --filter "Category!=Integration"
```

### Integration Tests (requires Qdrant running)
```powershell
# Start Qdrant first
docker run -p 6333:6333 qdrant/qdrant

# Run integration tests
dotnet test --filter "Category=Integration"
```

### Specific Test Class
```powershell
dotnet test --filter "FullyQualifiedName~KnowledgeRepositoryTests"
```

### With Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

### Unit Tests
- **QdrantVectorServiceTests**: Vector service construction and validation
- **KnowledgeRepositoryTests**: RAG functionality with mocked dependencies
  - Embedding generation on add
  - Vector search with fallback to text search
  - Embedding regeneration on update
  - Cleanup in both SQL and vector DB on delete

### Integration Tests
- **VectorSearchIntegrationTests**: Real Qdrant instance tests
  - Collection initialization
  - Store and retrieve embeddings
  - Similarity search accuracy
  - Deletion and cleanup

## Test Technologies

- **xUnit**: Test framework
- **FluentAssertions**: Readable assertion syntax
- **Moq**: Mocking framework for dependencies
- **InMemory Database**: EF Core in-memory provider for fast tests

## Writing New Tests

### Unit Test Example
```csharp
[Fact]
public async Task YourTest_Should_DoSomething()
{
    // Arrange
    var mockService = new Mock<IYourService>();
    mockService.Setup(x => x.Method()).ReturnsAsync(expectedValue);

    // Act
    var result = await _sut.MethodUnderTest();

    // Assert
    result.Should().Be(expectedValue);
    mockService.Verify(x => x.Method(), Times.Once);
}
```

### Integration Test Example
```csharp
[Fact(Skip = "Requires external service")]
[Trait("Category", "Integration")]
public async Task IntegrationTest_Should_Work()
{
    // Arrange - use real services
    var service = new RealService();

    // Act
    var result = await service.DoWork();

    // Assert
    result.Should().NotBeNull();
}
```

## Best Practices

1. **Isolation**: Each test should be independent
2. **Fast**: Unit tests should run in milliseconds
3. **Descriptive Names**: `Method_Should_Behavior_When_Condition`
4. **AAA Pattern**: Arrange, Act, Assert
5. **One Assertion**: Test one thing at a time
6. **Cleanup**: Dispose resources properly (implement IDisposable)

## Continuous Integration

Tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Unit Tests
  run: dotnet test --filter "Category!=Integration" --logger "trx"
  
- name: Run Integration Tests
  run: |
    docker-compose up -d qdrant
    dotnet test --filter "Category=Integration"
    docker-compose down
```

## Coverage Goals

- **Unit Tests**: 80%+ coverage for business logic
- **Integration Tests**: Critical paths (RAG search, embeddings)
- **Focus Areas**:
  - ChatService (AI orchestration)
  - KnowledgeRepository (RAG)
  - QdrantVectorService (vector operations)

## Troubleshooting

### Tests Fail to Run
```powershell
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Integration Tests Fail
```powershell
# Check Qdrant is running
curl http://localhost:6333/

# Restart Qdrant
docker restart <qdrant-container>
```

### InMemory Database Issues
- Each test gets a unique database (Guid in connection string)
- Database is disposed after each test
- No cleanup needed

## Future Enhancements

- [ ] Add ChatService end-to-end tests
- [ ] Add tool execution tests
- [ ] Add performance benchmarks
- [ ] Add load testing scenarios
- [ ] Add mutation testing
- [ ] Increase coverage to 90%+

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
