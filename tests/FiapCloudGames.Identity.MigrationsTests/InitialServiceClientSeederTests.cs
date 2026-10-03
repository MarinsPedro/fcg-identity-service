using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.MigrationsTests;

public sealed class InitialServiceClientSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldProvisionOnlyWebBffAndBeIdempotent()
    {
        await using var context = CreateContext();
        var options = ValidOptions();
        var seeder = CreateSeeder(context, options);

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var client = Assert.Single(await context.ServiceClients
            .Include(item => item.AllowedAudiences)
            .Include(item => item.AllowedScopes)
            .ToListAsync());
        Assert.Equal("web-bff", client.ClientId);
        Assert.True(new PasswordHasher().Verify("WebBffSecret1!", client.SecretHash));
        Assert.Contains(client.AllowedAudiences, item => item.Audience == "identity-api");
        Assert.Contains(client.AllowedScopes, item => item.Scope == "identity.clients.write");

        var auditEvent = Assert.Single(await context.SecurityAuditEvents.ToListAsync());
        Assert.Equal("service_client.provisioned", auditEvent.EventType);
        Assert.Equal("identity-migrations", auditEvent.ActorClientId);
        Assert.Equal("web-bff", auditEvent.SubjectId);
    }

    [Fact]
    public async Task SeedAsync_WithoutSecretOnFirstRun_ShouldFail()
    {
        await using var context = CreateContext();
        var options = ValidOptions();
        options.Secret = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context, options).SeedAsync(CancellationToken.None));

        Assert.Contains("InitialServiceClient:Secret", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.ServiceClients.ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_ForAnotherClient_ShouldFail()
    {
        await using var context = CreateContext();
        var options = ValidOptions();
        options.ClientId = "orders-api";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context, options).SeedAsync(CancellationToken.None));

        Assert.Contains("web-bff", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.ServiceClients.ToListAsync());
    }

    private static InitialServiceClientOptions ValidOptions() =>
        new()
        {
            ClientId = "web-bff",
            DisplayName = "Web BFF",
            Secret = "WebBffSecret1!",
            CanIssueUserTokens = true,
            AllowedAudiences = ["web-bff", "identity-api"],
            AllowedScopes = ["store.access", "identity.clients.write"]
        };

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"initial-client-seed-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    private static InitialServiceClientSeeder CreateSeeder(
        IdentityDbContext context,
        InitialServiceClientOptions options) =>
        new(
            context,
            new PasswordHasher(),
            Options.Create(options),
            TimeProvider.System,
            NullLogger<InitialServiceClientSeeder>.Instance);
}
