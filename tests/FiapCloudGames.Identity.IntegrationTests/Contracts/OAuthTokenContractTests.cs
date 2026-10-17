using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class OAuthTokenContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Emitir token OAuth - deve aceitar form e retornar resposta não armazenável")]
    public async Task Token_WithFormCredentials_ReturnsOAuthResponseAndNoStoreHeaders()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        using var response = await client.PostAsync(
            "/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "web-bff",
                ["client_secret"] = "IntegrationSecret1!",
                ["audience"] = "identity-api",
                ["scope"] = "identity.auth.login"
            }));
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Contains("no-cache", response.Headers.Pragma.ToString());
        Assert.False(string.IsNullOrWhiteSpace(
            document?["access_token"]?.GetValue<string>()));
        Assert.Equal("Bearer", document?["token_type"]?.GetValue<string>());
        Assert.True(document?["expires_in"]?.GetValue<int>() > 0);
        Assert.Equal(
            "identity.auth.login",
            document?["scope"]?.GetValue<string>());
        Assert.Null(document?["accessToken"]);
    }
}
