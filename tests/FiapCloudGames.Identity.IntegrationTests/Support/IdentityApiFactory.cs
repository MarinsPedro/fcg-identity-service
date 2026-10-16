using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.IntegrationTests.Support;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private static readonly DateTimeOffset SeedDateUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
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
            services.AddControllers()
                .AddApplicationPart(typeof(TransversalTestController).Assembly);
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
            ],
            SeedDateUtc);
        await context.ServiceClients.AddAsync(client);
        await context.SaveChangesAsync();
    }
}
