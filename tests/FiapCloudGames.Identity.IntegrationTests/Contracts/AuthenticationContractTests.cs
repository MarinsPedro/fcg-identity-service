using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class AuthenticationContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Acessar autenticação sem token de serviço - deve retornar não autorizado")]
    public async Task AuthenticationEndpoints_WithoutServiceToken_ReturnUnauthorized()
    {
        using var client = factory.CreateClient();
        using var loginResponse = await client.PostAsJsonAsync(
            "/v1/auth/login",
            new { email = "user@example.com", password = "Password1!" });
        using var refreshResponse = await client.PostAsJsonAsync(
            "/v1/auth/refresh",
            new { refreshToken = "refresh-token" });
        using var revokeResponse = await client.PostAsJsonAsync(
            "/v1/auth/revoke",
            new { refreshToken = "refresh-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, revokeResponse.StatusCode);
    }

    [Fact(DisplayName = "Acessar autenticação sem os escopos necessários - deve retornar proibido")]
    public async Task AuthenticationEndpoints_WithoutRequiredScopes_ReturnForbidden()
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
        var tokenDocument = await tokenResponse.Content.ReadFromJsonAsync<JsonObject>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenDocument?["access_token"]?.GetValue<string>());

        using var loginResponse = await client.PostAsJsonAsync(
            "/v1/auth/login",
            new { email = "user@example.com", password = "Password1!" });
        using var refreshResponse = await client.PostAsJsonAsync(
            "/v1/auth/refresh",
            new { refreshToken = "refresh-token" });
        using var revokeResponse = await client.PostAsJsonAsync(
            "/v1/auth/revoke",
            new { refreshToken = "refresh-token" });

        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, refreshResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revokeResponse.StatusCode);
    }

    [Fact(DisplayName = "Executar fluxo de autenticação - deve emitir e revogar tokens com credenciais válidas")]
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
                ["scope"] =
                    "identity.users.create identity.auth.login " +
                    "identity.auth.refresh identity.auth.revoke"
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
            "/v1/users",
            new
            {
                name = "Integration User",
                email = "integration@example.com",
                password = "Password1!"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var loginResponse = await client.PostAsJsonAsync(
            "/v1/auth/login",
            new
            {
                email = "integration@example.com",
                password = "Password1!"
            });
        loginResponse.EnsureSuccessStatusCode();
        var loginDocument = await loginResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.False(string.IsNullOrWhiteSpace(loginDocument?["accessToken"]?.GetValue<string>()));
        var refreshToken = loginDocument?["refreshToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        using var revokeResponse = await client.PostAsJsonAsync(
            "/v1/auth/revoke",
            new
            {
                refreshToken
            });
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/v1/auth/refresh",
            new
            {
                refreshToken
            });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
