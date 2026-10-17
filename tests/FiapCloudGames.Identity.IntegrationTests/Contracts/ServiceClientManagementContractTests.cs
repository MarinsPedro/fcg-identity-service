using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class ServiceClientManagementContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Gerenciar service client - deve criar, rotacionar, auditar e desativar o cliente")]
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
            "/v1/service-clients",
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
            "/v1/service-clients/orders-api");
        getResponse.EnsureSuccessStatusCode();
        var getDocument = await getResponse.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("orders-api", getDocument?["clientId"]?.GetValue<string>());
        Assert.Null(getDocument?["clientSecret"]);
        Assert.Null(getDocument?["secretHash"]);

        using var updateResponse = await client.PutAsJsonAsync(
            "/v1/service-clients/orders-api/permissions",
            new
            {
                allowedAudiences = new[] { "payments-api" },
                allowedScopes = new[] { "payments.write", "payments.read" }
            });
        updateResponse.EnsureSuccessStatusCode();

        using var rotateResponse = await client.PostAsync(
            "/v1/service-clients/orders-api/secret/rotate",
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
            "/v1/audit-events?subjectType=service_client&subjectId=orders-api&limit=2");
        auditResponse.EnsureSuccessStatusCode();
        var firstAuditPage = await auditResponse.Content.ReadFromJsonAsync<JsonObject>();
        var firstPageItems = firstAuditPage?["items"]?.AsArray();
        var nextCursor = firstAuditPage?["nextCursor"]?.GetValue<string>();
        Assert.Equal(2, firstPageItems?.Count);
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));

        using var nextAuditResponse = await client.GetAsync(
            $"/v1/audit-events?subjectType=service_client&subjectId=orders-api&limit=2&after={Uri.EscapeDataString(nextCursor!)}");
        nextAuditResponse.EnsureSuccessStatusCode();
        var secondAuditPage = await nextAuditResponse.Content.ReadFromJsonAsync<JsonObject>();
        var secondPageItems = secondAuditPage?["items"]?.AsArray();
        var auditEvents = firstPageItems!
            .Concat(secondPageItems!)
            .ToArray();
        Assert.Equal(
            auditEvents.Length,
            auditEvents.Select(item => item?["id"]?.GetValue<Guid>()).Distinct().Count());
        Assert.Contains(
            auditEvents,
            item => item?["eventType"]?.GetValue<string>() == "service_client.created");
        Assert.Contains(
            auditEvents,
            item => item?["eventType"]?.GetValue<string>() == "service_client.secret_rotated");
        Assert.All(
            auditEvents,
            item =>
            {
                Assert.Equal("service_client", item?["subjectType"]?.GetValue<string>());
                Assert.Equal("orders-api", item?["subjectId"]?.GetValue<string>());
            });

        var createdAuditEvent = auditEvents.Single(
            item => item?["eventType"]?.GetValue<string>() == "service_client.created");
        var occurredAtUtc = createdAuditEvent?["occurredAtUtc"]?.GetValue<DateTimeOffset>();
        var encodedOccurredAtUtc = Uri.EscapeDataString(occurredAtUtc?.ToString("O")!);
        using var filteredAuditResponse = await client.GetAsync(
            $"/v1/audit-events?eventType=service_client.created&actorClientId=web-bff&subjectType=service_client&subjectId=orders-api&occurredFromUtc={encodedOccurredAtUtc}&occurredToUtc={encodedOccurredAtUtc}");
        filteredAuditResponse.EnsureSuccessStatusCode();
        var filteredAuditPage = await filteredAuditResponse.Content
            .ReadFromJsonAsync<JsonObject>();
        var filteredEvent = Assert.Single(filteredAuditPage?["items"]?.AsArray()!);
        Assert.Equal(
            "service_client.created",
            filteredEvent?["eventType"]?.GetValue<string>());

        using var deactivateResponse = await client.DeleteAsync(
            "/v1/service-clients/orders-api");
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

    [Fact(DisplayName = "Consultar auditoria - deve rejeitar cursor inválido")]
    public async Task GetAuditEvents_WithInvalidCursor_ShouldReturnBadRequest()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        var token = await GetTokenAsync(
            client,
            "web-bff",
            "IntegrationSecret1!",
            "identity-api",
            "identity.audit.read");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(
            "/v1/audit-events?after=cursor-invalido");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact(DisplayName = "Consultar auditoria - deve rejeitar parâmetros fora do contrato HTTP")]
    public async Task GetAuditEvents_WithInvalidQueryParameters_ShouldReturnBadRequest()
    {
        using var client = factory.CreateClient();
        await factory.EnsureServiceClientAsync();
        var token = await GetTokenAsync(
            client,
            "web-bff",
            "IntegrationSecret1!",
            "identity-api",
            "identity.audit.read");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
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
