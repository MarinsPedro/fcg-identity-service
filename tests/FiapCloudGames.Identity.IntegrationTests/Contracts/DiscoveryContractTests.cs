using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class DiscoveryContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Consultar JWKS anonimamente - deve retornar a chave pública RSA")]
    public async Task Jwks_WhenRequestedAnonymously_ReturnsRsaPublicKey()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/.well-known/jwks.json");
        var document = await response.Content.ReadFromJsonAsync<JwksDocument>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(document);
        Assert.Single(document.Keys);
        Assert.Equal("RSA", document.Keys[0].Kty);
        Assert.False(string.IsNullOrWhiteSpace(document.Keys[0].N));
    }

    [Fact(DisplayName = "Consultar configuração OpenID anonimamente - deve publicar o endpoint OAuth2 de token")]
    public async Task OpenIdConfiguration_WhenRequestedAnonymously_PublishesOAuth2TokenEndpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/.well-known/openid-configuration");
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenEndpoint = document?["token_endpoint"]?.GetValue<string>();
        Assert.NotNull(tokenEndpoint);
        Assert.EndsWith("/oauth2/token", tokenEndpoint, StringComparison.Ordinal);
    }

    private sealed record JwksDocument(JwksKey[] Keys);
    private sealed record JwksKey(string Kty, string N);
}
