using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Host;

public sealed class ApiProblemDetailsTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Rejeitar requisição não autenticada - deve retornar Problem Details correlacionado")]
    public async Task UnauthorizedRequest_ShouldReturnCorrelatedProblemDetails()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "user@example.com",
                password = "Password1!"
            })
        };
        request.Headers.Add("X-Correlation-ID", "identity-test-401");

        using var response = await client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("identity-test-401", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(401, problem?["status"]?.GetValue<int>());
        Assert.Equal("identity-test-401", problem?["correlationId"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(problem?["traceId"]?.GetValue<string>()));
    }

    [Fact(DisplayName = "Receber JSON malformado - deve retornar Problem Details de validação sem expor conteúdo")]
    public async Task MalformedJson_ShouldReturnSafeValidationProblemDetails()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetServiceTokenAsync(client));
        const string malformedBody = "{\"email\":\"sensitive@example.com\",\"password\":\"secret\"";
        using var content = new StringContent(
            malformedBody,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync("/v1/auth/login", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonNode.Parse(responseBody)?.AsObject();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, problem?["status"]?.GetValue<int>());
        Assert.DoesNotContain("sensitive@example.com", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", responseBody, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tratar exceção inesperada - deve retornar erro interno sanitizado e correlacionado")]
    public async Task UnexpectedException_ShouldReturnSanitizedCorrelatedProblemDetails()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/_tests/unexpected-error");
        request.Headers.Add("X-Correlation-ID", "identity-test-500");

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        var problem = JsonNode.Parse(responseBody)?.AsObject();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, problem?["status"]?.GetValue<int>());
        Assert.Equal("identity-test-500", problem?["correlationId"]?.GetValue<string>());
        Assert.DoesNotContain("sensitive-internal-message", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", responseBody, StringComparison.Ordinal);
    }

    private async Task<string> GetServiceTokenAsync(HttpClient client)
    {
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
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonObject>();
        return token?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Token de teste não retornado.");
    }
}
