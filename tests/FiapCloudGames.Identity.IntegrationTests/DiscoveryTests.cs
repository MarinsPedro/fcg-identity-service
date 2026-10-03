using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;
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
    public async Task OpenIdConfiguration_PublishesOAuth2TokenEndpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            "/.well-known/openid-configuration");
        var document = await response.Content.ReadFromJsonAsync<JsonObject>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenEndpoint = document?["token_endpoint"]?.GetValue<string>();
        Assert.NotNull(tokenEndpoint);
        Assert.EndsWith(
            "/oauth2/token",
            tokenEndpoint,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_InvalidContract_ReturnsValidationProblem()
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
    public async Task FullFlow_IssuesServiceAndUserTokens()
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

    [Fact]
    public async Task HealthEndpoints_ReportLiveAndReady()
    {
        using var client = factory.CreateClient();

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
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
                ["Logging:LogLevel:Default"] = "Warning"
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

    public async Task EnsureServiceClientAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        if (await context.ServiceClients.AnyAsync(
            client => client.ClientId == "web-bff"))
        {
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var client = ServiceClient.Create(
            "web-bff",
            "Web BFF",
            passwordHasher.HashSecret("IntegrationSecret1!"),
            true,
            ["web-bff", "identity-api"],
            [
                "store.access",
                "identity.users.create",
                "identity.users.read",
                "identity.users.write",
                "identity.clients.create",
                "identity.clients.read",
                "identity.clients.write",
                "identity.audit.read"
            ],
            DateTimeOffset.UtcNow);
        await context.ServiceClients.AddAsync(client);
        await context.SaveChangesAsync();
    }
}
