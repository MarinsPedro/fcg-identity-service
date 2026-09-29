using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.IntegrationTests;

public sealed class DiscoveryTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task Jwks_IsPublicAndContainsRsaKey()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/.well-known/jwks.json");
        var document = await response.Content.ReadFromJsonAsync<JwksDocument>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(document);
        Assert.Single(document.Keys);
        Assert.Equal("RSA", document.Keys[0].Kty);
        Assert.False(string.IsNullOrWhiteSpace(document.Keys[0].N));
    }

    [Fact]
    public async Task FullFlow_IssuesServiceAndUserTokens()
    {
        using var client = factory.CreateClient();
        using var tokenResponse = await client.PostAsync(
            "/connect/token",
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
        var serviceToken = tokenDocument?["accessToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(serviceToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceToken);
        using var createResponse = await client.PostAsJsonAsync(
            "/internal/users",
            new
            {
                name = "Integration User",
                email = "integration@example.com",
                password = "Password1!"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
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
            "/api/auth/revoke",
            new
            {
                refreshToken,
                clientId = "web-bff",
                clientSecret = "IntegrationSecret1!"
            });
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                refreshToken,
                clientId = "web-bff",
                clientSecret = "IntegrationSecret1!"
            });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    private sealed record JwksDocument(JwksKey[] Keys);
    private sealed record JwksKey(string Kty, string N);
}

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"identity-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] =
                    "Host=localhost;Database=integration;Username=test;Password=test",
                ["Jwt:Issuer"] = "integration-issuer",
                ["Jwt:PrivateKeyPath"] = null,
                ["Jwt:AllowEphemeralDevelopmentKey"] = "true",
                ["Identity:BootstrapClients:0:ClientId"] = "web-bff",
                ["Identity:BootstrapClients:0:DisplayName"] = "Web BFF",
                ["Identity:BootstrapClients:0:Secret"] = "IntegrationSecret1!",
                ["Identity:BootstrapClients:0:CanIssueUserTokens"] = "true",
                ["Identity:BootstrapClients:0:AllowedAudiences:0"] = "web-bff",
                ["Identity:BootstrapClients:0:AllowedAudiences:1"] = "identity-api",
                ["Identity:BootstrapClients:0:AllowedScopes:0"] = "store.access",
                ["Identity:BootstrapClients:0:AllowedScopes:1"] = "identity.users.create"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
