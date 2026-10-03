using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class AuthenticationContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task Login_WithInvalidContract_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "invalid",
                password = string.Empty,
                clientId = string.Empty,
                clientSecret = string.Empty
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AuthenticationFlow_WithValidCredentials_IssuesAndRevokesTokens()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        using var tokenResponse = await client.PostAsync(
            "/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "web-bff",
                ["client_secret"] = "IntegrationSecret1!",
                ["audience"] = "identity-api",
                ["scope"] = "identity.users.create"
            }));
        tokenResponse.EnsureSuccessStatusCode();
        Assert.Contains("no-store", tokenResponse.Headers.CacheControl?.ToString());
        Assert.Contains("no-cache", tokenResponse.Headers.Pragma.ToString());
        var tokenDocument = await tokenResponse.Content.ReadFromJsonAsync<JsonObject>();
        var serviceToken = tokenDocument?["access_token"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(serviceToken));
        Assert.Equal("Bearer", tokenDocument?["token_type"]?.GetValue<string>());
        Assert.True(tokenDocument?["expires_in"]?.GetValue<int>() > 0);
        Assert.Null(tokenDocument?["accessToken"]);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceToken);
        using var createResponse = await client.PostAsJsonAsync(
            "/internal/v1/users",
            new
            {
                name = "Integration User",
                email = "integration@example.com",
                password = "Password1!"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = "integration@example.com",
                password = "Password1!",
                clientId = "web-bff",
                clientSecret = "IntegrationSecret1!"
            });
        loginResponse.EnsureSuccessStatusCode();
        var loginDocument = await loginResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.False(string.IsNullOrWhiteSpace(loginDocument?["accessToken"]?.GetValue<string>()));
        var refreshToken = loginDocument?["refreshToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        using var revokeResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/revoke",
            new
            {
                refreshToken,
                clientId = "web-bff",
                clientSecret = "IntegrationSecret1!"
            });
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new
            {
                refreshToken,
                clientId = "web-bff",
                clientSecret = "IntegrationSecret1!"
            });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
