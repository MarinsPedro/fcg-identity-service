using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace FiapCloudGames.Identity.IntegrationTests;

public sealed class ServiceClientManagementTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task ManagementFlow_ShouldCreateRotateAuditAndDeactivateClient()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        var administratorToken = await GetTokenAsync(
            client,
            "web-bff",
            "IntegrationSecret1!",
            "identity-api",
            "identity.clients.create identity.clients.read identity.clients.write identity.audit.read");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", administratorToken);

        using var createResponse = await client.PostAsJsonAsync(
            "/internal/v1/service-clients",
            new
            {
                clientId = "orders-api",
                displayName = "Orders API",
                canIssueUserTokens = false,
                allowedAudiences = new[] { "payments-api" },
                allowedScopes = new[] { "payments.write" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createDocument = await createResponse.Content.ReadFromJsonAsync<JsonObject>();
        var initialSecret = createDocument?["clientSecret"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(initialSecret));
        Assert.Null(createDocument?["client"]?["secretHash"]);

        using var getResponse = await client.GetAsync(
            "/internal/v1/service-clients/orders-api");
        getResponse.EnsureSuccessStatusCode();
        var getDocument = await getResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("orders-api", getDocument?["clientId"]?.GetValue<string>());
        Assert.Null(getDocument?["clientSecret"]);
        Assert.Null(getDocument?["secretHash"]);

        using var updateResponse = await client.PutAsJsonAsync(
            "/internal/v1/service-clients/orders-api/permissions",
            new
            {
                allowedAudiences = new[] { "payments-api" },
                allowedScopes = new[] { "payments.write", "payments.read" }
            });
        updateResponse.EnsureSuccessStatusCode();

        using var rotateResponse = await client.PostAsync(
            "/internal/v1/service-clients/orders-api/secret/rotate",
            content: null);
        rotateResponse.EnsureSuccessStatusCode();
        var rotateDocument = await rotateResponse.Content.ReadFromJsonAsync<JsonObject>();
        var rotatedSecret = rotateDocument?["clientSecret"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(rotatedSecret));
        Assert.NotEqual(initialSecret, rotatedSecret);

        client.DefaultRequestHeaders.Authorization = null;
        using var oldSecretResponse = await RequestTokenAsync(
            client,
            "orders-api",
            initialSecret!,
            "payments-api",
            "payments.write");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSecretResponse.StatusCode);

        var ordersToken = await GetTokenAsync(
            client,
            "orders-api",
            rotatedSecret!,
            "payments-api",
            "payments.write");
        Assert.False(string.IsNullOrWhiteSpace(ordersToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", administratorToken);
        using var auditResponse = await client.GetAsync(
            "/internal/v1/audit-events?take=20");
        auditResponse.EnsureSuccessStatusCode();
        var auditEvents = await auditResponse.Content.ReadFromJsonAsync<JsonArray>();
        Assert.Contains(
            auditEvents!,
            item => item?["eventType"]?.GetValue<string>() == "service_client.created");
        Assert.Contains(
            auditEvents!,
            item => item?["eventType"]?.GetValue<string>() == "service_client.secret_rotated");

        using var deactivateResponse = await client.DeleteAsync(
            "/internal/v1/service-clients/orders-api");
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var inactiveResponse = await RequestTokenAsync(
            client,
            "orders-api",
            rotatedSecret!,
            "payments-api",
            "payments.write");
        Assert.Equal(HttpStatusCode.Unauthorized, inactiveResponse.StatusCode);
    }

    private static async Task<string> GetTokenAsync(
        HttpClient client,
        string clientId,
        string clientSecret,
        string audience,
        string scope)
    {
        using var response = await RequestTokenAsync(
            client,
            clientId,
            clientSecret,
            audience,
            scope);
        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();
        return document?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Token não retornado pelo Identity.");
    }

    private static Task<HttpResponseMessage> RequestTokenAsync(
        HttpClient client,
        string clientId,
        string clientSecret,
        string audience,
        string scope) =>
        client.PostAsync(
            "/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["audience"] = audience,
                ["scope"] = scope
            }));
}
