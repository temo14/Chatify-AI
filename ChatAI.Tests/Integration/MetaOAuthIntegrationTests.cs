namespace ChatAI.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChatAI.Api.DTOs.MetaOAuth;
using ChatAI.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests for Meta OAuth flow
/// NOTE: Requires CustomWebApplicationFactory setup (not yet implemented)
/// TODO: Create ChatAI.Tests/Setup/CustomWebApplicationFactory.cs with WebApplicationFactory<Program>
/// TODO: Add AuthenticateAsTestTenantAsync helper method
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "MetaOAuth")]
public class MetaOAuthIntegrationTests // : IClassFixture<CustomWebApplicationFactory>
{
    // TODO: Uncomment and implement when CustomWebApplicationFactory is created
    /*
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    public MetaOAuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    */
    
    /*
    // INTEGRATION TESTS - Uncomment when CustomWebApplicationFactory is implemented
    
    [Fact]
    public async Task InitiateOAuth_WithAuthenticatedUser_ReturnsAuthorizationUrl()
    {
        // Arrange
        await _factory.AuthenticateAsTestTenantAsync(_client);
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Messenger };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        result.Should().NotBeNull();
        result!.AuthorizationUrl.Should().Contain("facebook.com/dialog/oauth");
        result.AuthorizationUrl.Should().Contain("client_id=");
        result.AuthorizationUrl.Should().Contain("redirect_uri=");
        result.AuthorizationUrl.Should().Contain("state=");
        result.State.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task InitiateOAuth_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Messenger };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task InitiateOAuth_StoresStateInCache()
    {
        // Arrange
        await _factory.AuthenticateAsTestTenantAsync(_client);
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Instagram };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/tenant/meta/instagram/oauth/initiate", request);
        var result = await response.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        // Assert
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        
        var cacheKey = $"oauth_state:{result!.State}";
        var stateJson = await cache.GetStringAsync(cacheKey);
        
        stateJson.Should().NotBeNullOrEmpty();
        
        // Verify state content
        var state = JsonSerializer.Deserialize<dynamic>(stateJson!);
        state.Should().NotBeNull();
    }
    
    [Fact]
    public async Task OAuthCallback_WithoutCodeOrState_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/meta/oauth/callback");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task OAuthCallback_WithInvalidState_RedirectsWithError()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/meta/oauth/callback?code=test&state=invalid");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Redirects return 200
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("oauth-error=");
    }
    
    [Theory]
    [InlineData("Messenger", "pages_messaging")]
    [InlineData("Instagram", "instagram_basic")]
    [InlineData("WhatsApp", "whatsapp_business_messaging")]
    public async Task InitiateOAuth_IncludesChannelSpecificScopes(string channel, string expectedScope)
    {
        // Arrange
        await _factory.AuthenticateAsTestTenantAsync(_client);
        var request = new OAuthInitiateRequestDto { MetaChannel = Enum.Parse<MetaChannel>(channel) };
        
        // Act
        var response = await _client.PostAsJsonAsync($"/api/tenant/meta/{channel.ToLower()}/oauth/initiate", request);
        var result = await response.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        // Assert
        result!.AuthorizationUrl.Should().Contain($"scope=");
        result.AuthorizationUrl.Should().Contain(expectedScope);
    }
    
    [Fact]
    public async Task InitiateOAuth_GeneratesUniqueNonce()
    {
        // Arrange
        await _factory.AuthenticateAsTestTenantAsync(_client);
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Messenger };
        
        // Act
        var response1 = await _client.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        var result1 = await response1.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        var response2 = await _client.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        var result2 = await response2.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        // Assert
        result1!.State.Should().NotBe(result2!.State);
    }
    
    [Fact]
    public async Task InitiateOAuth_MultiTenant_IsolatesStateByTenant()
    {
        // Arrange
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();
        
        await _factory.AuthenticateAsTestTenantAsync(client1);
        await _factory.AuthenticateAsTestTenantAsync(client2, tenantId: Guid.NewGuid().ToString());
        
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Messenger };
        
        // Act
        var response1 = await client1.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        var result1 = await response1.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        var response2 = await client2.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request);
        var result2 = await response2.Content.ReadFromJsonAsync<OAuthInitiateResponseDto>();
        
        // Assert
        result1!.State.Should().NotBe(result2!.State);
        
        // Verify each state is isolated
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        
        var state1 = await cache.GetStringAsync($"oauth_state:{result1.State}");
        var state2 = await cache.GetStringAsync($"oauth_state:{result2.State}");
        
        state1.Should().NotBeNullOrEmpty();
        state2.Should().NotBeNullOrEmpty();
        state1.Should().NotBe(state2);
    }
    
    [Fact]
    public async Task OAuthCallback_WithMetaError_RedirectsWithErrorMessage()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/meta/oauth/callback?error=access_denied&error_description=User+denied+permission");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("oauth-error=");
        content.Should().Contain("User+denied+permission");
    }
    
    [Fact]
    public async Task InitiateOAuth_RateLimiting_PreventsAbuse()
    {
        // Arrange
        await _factory.AuthenticateAsTestTenantAsync(_client);
        var request = new OAuthInitiateRequestDto { MetaChannel = MetaChannel.Messenger };
        
        // Act - Make multiple rapid requests
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _client.PostAsJsonAsync("/api/tenant/meta/messenger/oauth/initiate", request)
        );
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert - At least some should succeed (rate limiting specifics depend on configuration)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        successCount.Should().BeGreaterThan(0);
    }
    */
}
