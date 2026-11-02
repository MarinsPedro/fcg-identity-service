using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class SecurityAuditHttpContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Consultar auditoria - deve aplicar as restrições do binding HTTP")]
    public async Task GetAuditEvents_WithInvalidQueryParameters_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                await GetAuditTokenAsync(client));
        var requests = new Dictionary<string, string>
        {
            ["/v1/audit-events?limit=0"] = "limit",
            [$"/v1/audit-events?eventType={new string('a', 101)}"] = "eventType",
            [$"/v1/audit-events?after={new string('a', 2049)}"] = "after"
        };

        foreach (var (requestUri, expectedField) in requests)
        {
            using var response = await client.GetAsync(requestUri);
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            Assert.Contains(expectedField, responseBody, StringComparison.Ordinal);
        }
    }

    private static async Task<string> GetAuditTokenAsync(HttpClient client)
    {
        using var response = await client.PostAsync(
            "/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "web-bff",
                ["client_secret"] = "IntegrationSecret1!",
                ["audience"] = "identity-api",
                ["scope"] = "identity.audit.read"
            }));
        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();
        return document?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Token não retornado pelo Identity.");
    }
}
