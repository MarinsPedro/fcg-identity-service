using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.IntegrationTests.Components.Seeding;

public sealed class InitialServiceClientSeederTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Semear service client inicial - deve provisionar somente o BFF e ser idempotente")]
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
        Assert.Contains(client.AllowedScopes, item => item.Scope == "identity.auth.login");
        Assert.Contains(client.AllowedScopes, item => item.Scope == "identity.clients.write");

        var auditEvent = Assert.Single(await context.SecurityAuditEvents.ToListAsync());
        Assert.Equal("service_client.provisioned", auditEvent.EventType);
        Assert.Equal("identity-migrations", auditEvent.ActorClientId);
        Assert.Equal("web-bff", auditEvent.SubjectId);
    }

    [Fact(DisplayName = "Semear service client existente - deve adicionar permissões ausentes sem trocar segredo")]
    public async Task SeedAsync_ExistingClient_ShouldAddMissingPermissionsWithoutChangingSecret()
    {
        await using var context = CreateContext();
        var passwordHasher = new PasswordHasher();
        var existingClient = ServiceClient.Create(
            "web-bff",
            "Web BFF legado",
            passwordHasher.HashSecret("ExistingSecret1!"),
            true,
            ["identity-api", "legacy-api"],
            ["identity.users.create", "legacy.scope"],
            NowUtc.AddDays(-1));
        await context.ServiceClients.AddAsync(existingClient);
        await context.SaveChangesAsync();

        var options = ValidOptions();
        options.Secret = null;
        var seeder = CreateSeeder(context, options);

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var client = Assert.Single(await context.ServiceClients
            .Include(item => item.AllowedAudiences)
            .Include(item => item.AllowedScopes)
            .ToListAsync());
        Assert.True(passwordHasher.Verify("ExistingSecret1!", client.SecretHash));
        Assert.Contains(client.AllowedAudiences, item => item.Audience == "legacy-api");
        Assert.Contains(client.AllowedAudiences, item => item.Audience == "web-bff");
        Assert.Contains(client.AllowedScopes, item => item.Scope == "legacy.scope");
        Assert.All(
            options.AllowedScopes,
            scope => Assert.Contains(client.AllowedScopes, item => item.Scope == scope));

        var auditEvent = Assert.Single(await context.SecurityAuditEvents.ToListAsync());
        Assert.Equal("service_client.permissions_changed", auditEvent.EventType);
        Assert.Equal("identity-migrations", auditEvent.ActorClientId);
        Assert.Equal("web-bff", auditEvent.SubjectId);
    }

    [Fact(DisplayName = "Semear service client inicial - deve falhar sem segredo na primeira execução")]
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

    [Fact(DisplayName = "Semear service client inicial - deve falhar para outro client ID")]
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
            AllowedScopes =
            [
                "store.access",
                "identity.auth.login",
                "identity.auth.refresh",
                "identity.auth.revoke",
                "identity.users.create",
                "identity.users.read",
                "identity.users.write",
                "identity.clients.create",
                "identity.clients.read",
                "identity.clients.write",
                "identity.audit.read"
            ]
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
            new FixedTimeProvider(NowUtc),
            NullLogger<InitialServiceClientSeeder>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
