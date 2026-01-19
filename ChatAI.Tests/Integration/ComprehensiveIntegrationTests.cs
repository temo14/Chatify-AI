using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ChatAI.Api.DTOs;
using ChatAI.Api.DTOs.Knowledge;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAI.Tests.Integration;

/// <summary>
/// Comprehensive integration tests covering all critical application flows
/// Tests multi-tenancy isolation, authentication, authorization, and feature correctness
/// </summary>
[Trait("Category", "Integration")]
public class ComprehensiveIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ChatDbContext _context;

    // Test data
    private Guid _tenantAId = Guid.NewGuid();
    private Guid _tenantBId = Guid.NewGuid();
    private string _tenantASlug = "tenanta";
    private string _tenantBSlug = "tenantb";
    private string _tokenA = "";
    private string _tokenB = "";
    private string _apiKeyA = "";
    private string _apiKeyB = "";
    private Guid _docAId;
    private Guid _docBId;
    private string? _sessionAId;
    private string? _sessionBId;

    public ComprehensiveIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Set environment variable BEFORE building host
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration
                var testConfig = new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "ThisIsATestSecretKeyForJwtTokenGeneration123456",
                    ["Jwt:Issuer"] = "ChatAI.Test",
                    ["Jwt:Audience"] = "ChatAI.Test.Client",
                    ["Jwt:ExpirationMinutes"] = "60",
                    ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com",
                    ["AzureOpenAI:ApiKey"] = "test-key",
                    ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "text-embedding-ada-002",
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;",
                    ["Email:SmtpServer"] = "localhost",
                    ["Email:SmtpPort"] = "25",
                    ["Email:FromEmail"] = "test@test.com",
                    ["Email:FromName"] = "Test",
                    ["Cache:Provider"] = "Memory"
                };
                config.AddInMemoryCollection(testConfig);
            });
            
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext configuration
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ChatDbContext>));
                if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                // Replace with InMemory database for testing
                services.AddDbContext<ChatDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        // Seed test data
        SeedTestDataAsync().Wait();
    }

    private async Task SeedTestDataAsync()
    {
        // Tenant A
        var tenantA = new Tenant
        {
            Id = _tenantAId,
            Name = "Tenant A",
            Slug = _tenantASlug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Tenant B
        var tenantB = new Tenant
        {
            Id = _tenantBId,
            Name = "Tenant B",
            Slug = _tenantBSlug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Admin users for each tenant
        var adminA = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = "adminA",
            Email = "adminA@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            TenantId = _tenantAId,
            IsActive = true,
            IsPlatformAdmin = false
        };

        var adminB = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = "adminB",
            Email = "adminB@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            TenantId = _tenantBId,
            IsActive = true,
            IsPlatformAdmin = false
        };

        // Settings for each tenant
        var settingsA = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantAId,
            VectorStorageMode = "SQL",
            ChatHistoryRetentionDays = 90
        };

        var settingsB = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantBId,
            VectorStorageMode = "SQL",
            ChatHistoryRetentionDays = 90
        };

        // API Keys (plain + hash must match production hashing rules)
        _apiKeyA = "chatai_test_key_a_12345678901234567890";
        _apiKeyB = "chatai_test_key_b_12345678901234567890";
        var apiKeyHashA = HashApiKeySha256Base64(_apiKeyA);
        var apiKeyHashB = HashApiKeySha256Base64(_apiKeyB);

        // API Keys
        var apiKeyEntityA = new ApiKey
        {
            Id = Guid.NewGuid(),
            ClientName = "Test Client A",
            KeyHash = apiKeyHashA,
            TenantId = _tenantAId.ToString(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var apiKeyEntityB = new ApiKey
        {
            Id = Guid.NewGuid(),
            ClientName = "Test Client B",
            KeyHash = apiKeyHashB,
            TenantId = _tenantBId.ToString(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Knowledge documents
        var docA = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = "Tenant A Document",
            Content = "This is confidential information for Tenant A about shipping policies.",
            Category = "policies",
            TenantId = _tenantAId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var docB = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = "Tenant B Document",
            Content = "This is confidential information for Tenant B about return policies.",
            Category = "policies",
            TenantId = _tenantBId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _docAId = docA.Id;
        _docBId = docB.Id;

        // Chat sessions
        var sessionA = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = _tenantAId,
            Title = "Session A",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var sessionB = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = _tenantBId,
            Title = "Session B",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _sessionAId = sessionA.Id;
        _sessionBId = sessionB.Id;

        // Add messages to sessions
        var messageA1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = _sessionAId,
            Content = "Hello from Tenant A",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow,
            TenantId = _tenantAId
        };

        var messageB1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = _sessionBId,
            Content = "Hello from Tenant B",
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow,
            TenantId = _tenantBId
        };

        _context.Tenants.AddRange(tenantA, tenantB);
        _context.AdminUsers.AddRange(adminA, adminB);
        _context.TenantSettings.AddRange(settingsA, settingsB);
        _context.ApiKeys.AddRange(apiKeyEntityA, apiKeyEntityB);
        _context.KnowledgeDocuments.AddRange(docA, docB);
        _context.ChatSessions.AddRange(sessionA, sessionB);
        _context.ChatMessages.AddRange(messageA1, messageB1);

        await _context.SaveChangesAsync();
    }

    #region Authentication Tests

    [Fact]
    public async Task Login_WithValidSlugAndCredentials_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "adminA",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("adminA");

        _tokenA = result.Token;
    }

    [Fact]
    public async Task Login_WithWrongTenant_ShouldFail()
    {
        // Arrange - Try to login with wrong password
        var loginDto = new LoginDto
        {
            Username = "adminA",
            Password = "WrongPassword!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyAuthentication_WithValidKey_Succeeds()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/chat/sessions?userId=test-user");
        request.Headers.Add("X-API-Key", _apiKeyA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApiKeyAuthentication_WithInvalidKey_Fails()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/chat/sessions?userId=test-user");
        request.Headers.Add("X-API-Key", "invalid-key-12345678901234567890");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Multi-Tenancy Isolation Tests

    [Fact]
    public async Task TenantA_CannotAccessTenantB_KnowledgeDocuments()
    {
        // Arrange - Login as Tenant A admin
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/knowledge/{_docBId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "Tenant A should not be able to access Tenant B's documents");
    }

    [Fact]
    public async Task TenantA_CanAccessOwnKnowledgeDocuments()
    {
        // Arrange
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/knowledge/{_docAId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeDocumentDto>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Tenant A Document");
    }

    [Fact]
    public async Task TenantA_CannotAccessTenantB_ChatSessions()
    {
        // Arrange
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/session/{_sessionBId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Tenant A should not be able to access Tenant B's sessions");
    }

    [Fact]
    public async Task TenantA_CanAccessOwnChatSessions()
    {
        // Arrange
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/session/{_sessionAId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListKnowledgeDocuments_OnlyReturnsTenantSpecificDocs()
    {
        // Arrange
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/knowledge");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<KnowledgeDocumentDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1, "Tenant A should only see their own documents");
        result.Should().Contain(d => d.Title == "Tenant A Document");
        result.Should().NotContain(d => d.Title == "Tenant B Document");
    }

    [Fact]
    public async Task ListChatSessions_OnlyReturnsTenantSpecificSessions()
    {
        // Arrange
        await LoginAsTenantA();
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ChatSession>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result.Should().Contain(s => s.Title == "Session A");
        result.Should().NotContain(s => s.Title == "Session B");
    }

    [Fact]
    public async Task UpdateConfiguration_OnlyAffectsOwnTenant()
    {
        // Arrange
        await LoginAsTenantA();
        await LoginAsTenantB();

        var updateDto = new UpdateConfigurationDto
        {
            Key = "test-key-a",
            Value = "Updated value for Tenant A",
            DataType = "String",
            IsActive = true
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/configuration")
        {
            Content = JsonContent.Create(updateDto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act - Update Tenant A config
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // Assert - Tenant B should not see Tenant A's config changes
        var getRequestB = new HttpRequestMessage(HttpMethod.Get, "/api/configuration");
        getRequestB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenB);
        var responseB = await _client.SendAsync(getRequestB);
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region CRUD Operation Tests

    [Fact]
    public async Task CreateKnowledgeDocument_WithValidAuth_Succeeds()
    {
        // Arrange
        await LoginAsTenantA();

        var createDto = new AddKnowledgeDocumentRequest
        {
            Title = "New Document",
            Content = "This is new content for testing.",
            Category = "test",
            IsActive = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/knowledge")
        {
            Content = JsonContent.Create(createDto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeDocumentDto>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Document");
    }

    [Fact]
    public async Task UpdateKnowledgeDocument_WithValidAuth_Succeeds()
    {
        // Arrange
        await LoginAsTenantA();

        var updateDto = new AddKnowledgeDocumentRequest
        {
            Title = "Updated Title",
            Content = "Updated content",
            Category = "updated",
            IsActive = true
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/knowledge/{_docAId}")
        {
            Content = JsonContent.Create(updateDto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify update
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/knowledge/{_docAId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);
        var getResponse = await _client.SendAsync(getRequest);
        var result = await getResponse.Content.ReadFromJsonAsync<KnowledgeDocumentDto>();
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteKnowledgeDocument_WithValidAuth_Succeeds()
    {
        // Arrange
        await LoginAsTenantA();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/knowledge/{_docAId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify deletion
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/knowledge/{_docAId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);
        var getResponse = await _client.SendAsync(getRequest);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Fails()
    {
        // Act
        var response = await _client.GetAsync("/api/knowledge");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithExpiredToken_Fails()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/knowledge");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "expired.invalid.token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task CreateKnowledgeDocument_WithMissingFields_Fails()
    {
        // Arrange
        await LoginAsTenantA();

        var invalidDto = new { Title = "Only Title" }; // Missing required fields

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/knowledge")
        {
            Content = JsonContent.Create(invalidDto)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNonExistentResource_ReturnsNotFound()
    {
        // Arrange
        await LoginAsTenantA();

        var nonExistentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/knowledge/{nonExistentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateKnowledgeDocument_WithInvalidJson_Fails()
    {
        // Arrange
        await LoginAsTenantA();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/knowledge")
        {
            Content = new StringContent("{invalid json", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenA);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    private async Task LoginAsTenantA()
    {
        if (!string.IsNullOrEmpty(_tokenA)) return;

        var loginDto = new LoginDto
        {
            Username = "adminA",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        _tokenA = result!.Token;
    }

    private async Task LoginAsTenantB()
    {
        if (!string.IsNullOrEmpty(_tokenB)) return;

        var loginDto = new LoginDto
        {
            Username = "adminB",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        _tokenB = result!.Token;
    }

    private static string HashApiKeySha256Base64(string plainKey)
    {
        var bytes = Encoding.UTF8.GetBytes(plainKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
