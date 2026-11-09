using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.IntegrationTests.Components.Seeding;

public sealed class InitialServiceClientsSeederTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Semear service clients iniciais - deve provisionar BFF e Orders e ser idempotente")]
    public async Task SeedAsync_ShouldProvisionConfiguredClientsAndBeIdempotent()
    {
        await using var context = CreateContext();
        var options = ValidOptions();
        var seeder = CreateSeeder(context, options);

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var clients = await context.ServiceClients
            .Include(item => item.AllowedAudiences)
            .Include(item => item.AllowedScopes)
            .OrderBy(item => item.ClientId)
            .ToListAsync();
        Assert.Equal(2, clients.Count);

        var orders = clients[0];
        Assert.Equal("orders-api", orders.ClientId);
        Assert.False(orders.CanIssueUserTokens);
        Assert.True(new PasswordHasher().Verify("OrdersSecret1!", orders.SecretHash));
        Assert.Equal(
            ["catalog-api", "library-api", "promotions-api"],
            orders.AllowedAudiences.Select(item => item.Audience).Order());
        Assert.Equal(
            [
                "catalog.games.read",
                "library.ownership.read",
                "promotions.calculate",
                "promotions.campaigns.read"
            ],
            orders.AllowedScopes.Select(item => item.Scope).Order());

        var webBff = clients[1];
        Assert.Equal("web-bff", webBff.ClientId);
        Assert.True(webBff.CanIssueUserTokens);
        Assert.True(new PasswordHasher().Verify("WebBffSecret1!", webBff.SecretHash));
        Assert.Contains(webBff.AllowedAudiences, item => item.Audience == "identity-api");
        Assert.Contains(webBff.AllowedScopes, item => item.Scope == "identity.auth.login");

        var auditEvents = await context.SecurityAuditEvents
            .OrderBy(item => item.SubjectId)
            .ToListAsync();
        Assert.Equal(2, auditEvents.Count);
        Assert.All(
            auditEvents,
            auditEvent => Assert.Equal("service_client.provisioned", auditEvent.EventType));
        Assert.Equal(["orders-api", "web-bff"], auditEvents.Select(item => item.SubjectId));
    }

    [Fact(DisplayName = "Semear service client existente - deve sincronizar toda a configuração")]
    public async Task SeedAsync_ExistingClient_ShouldSynchronizeCompleteConfiguration()
    {
        await using var context = CreateContext();
        var passwordHasher = new PasswordHasher();
        var existingClient = ServiceClient.Create(
            "web-bff",
            "Web BFF legado",
            passwordHasher.HashSecret("ExistingSecret1!"),
            false,
            ["identity-api", "legacy-api"],
            ["identity.users.create", "legacy.scope"],
            NowUtc.AddDays(-1));
        existingClient.Deactivate();
        await context.ServiceClients.AddAsync(existingClient);
        await context.SaveChangesAsync();

        var webBffOptions = WebBffOptions();
        var options = new InitialServiceClientsOptions { Clients = [webBffOptions] };
        var seeder = CreateSeeder(context, options);

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var client = Assert.Single(await context.ServiceClients
            .Include(item => item.AllowedAudiences)
            .Include(item => item.AllowedScopes)
            .ToListAsync());
        Assert.Equal("Web BFF", client.DisplayName);
        Assert.True(client.CanIssueUserTokens);
        Assert.True(client.IsActive);
        Assert.True(passwordHasher.Verify("WebBffSecret1!", client.SecretHash));
        Assert.False(passwordHasher.Verify("ExistingSecret1!", client.SecretHash));
        Assert.DoesNotContain(client.AllowedAudiences, item => item.Audience == "legacy-api");
        Assert.DoesNotContain(client.AllowedScopes, item => item.Scope == "legacy.scope");
        Assert.Equal(
            webBffOptions.AllowedAudiences.Order(),
            client.AllowedAudiences.Select(item => item.Audience).Order());
        Assert.Equal(
            webBffOptions.AllowedScopes.Order(),
            client.AllowedScopes.Select(item => item.Scope).Order());

        var auditEvent = Assert.Single(await context.SecurityAuditEvents.ToListAsync());
        Assert.Equal("service_client.provisioning_synchronized", auditEvent.EventType);
        Assert.Equal("identity-migrations", auditEvent.ActorClientId);
        Assert.Equal("web-bff", auditEvent.SubjectId);
    }

    [Fact(DisplayName = "Semear service clients iniciais - deve falhar sem segredo na primeira execução")]
    public async Task SeedAsync_WithoutSecretOnFirstRun_ShouldFail()
    {
        await using var context = CreateContext();
        var options = new InitialServiceClientsOptions
        {
            Clients = [OrdersOptions()]
        };
        options.Clients[0].Secret = null;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context, options).SeedAsync(CancellationToken.None));

        Assert.Contains("orders-api", exception.Message, StringComparison.Ordinal);
        Assert.Contains("InitialServiceClients:Clients", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.ServiceClients.ToListAsync());
    }

    [Fact(DisplayName = "Semear service clients iniciais - deve falhar para client ID duplicado")]
    public async Task SeedAsync_WithDuplicateClientId_ShouldFail()
    {
        await using var context = CreateContext();
        var options = new InitialServiceClientsOptions
        {
            Clients = [OrdersOptions(), OrdersOptions()]
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context, options).SeedAsync(CancellationToken.None));

        Assert.Contains("orders-api", exception.Message, StringComparison.Ordinal);
        Assert.Contains("duplicado", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await context.ServiceClients.ToListAsync());
    }

    private static InitialServiceClientsOptions ValidOptions() =>
        new()
        {
            Clients = [WebBffOptions(), OrdersOptions()]
        };

    private static InitialServiceClientOptions WebBffOptions() =>
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

    private static InitialServiceClientOptions OrdersOptions() =>
        new()
        {
            ClientId = "orders-api",
            DisplayName = "Orders API",
            Secret = "OrdersSecret1!",
            CanIssueUserTokens = false,
            AllowedAudiences = ["catalog-api", "promotions-api", "library-api"],
            AllowedScopes =
            [
                "catalog.games.read",
                "promotions.calculate",
                "promotions.campaigns.read",
                "library.ownership.read"
            ]
        };

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"initial-client-seed-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    private static InitialServiceClientsSeeder CreateSeeder(
        IdentityDbContext context,
        InitialServiceClientsOptions options) =>
        new(
            context,
            new PasswordHasher(),
            Options.Create(options),
            new FixedTimeProvider(NowUtc),
            NullLogger<InitialServiceClientsSeeder>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
