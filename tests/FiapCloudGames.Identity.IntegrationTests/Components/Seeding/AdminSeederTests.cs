using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.IntegrationTests.Components.Seeding;

public sealed class AdminSeederTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Semear administrador - deve criar o usuário somente uma vez")]
    public async Task SeedAsync_ShouldCreateAdministratorOnlyOnce()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(
            context,
            new AdminSeedOptions
            {
                Name = "Administrador Local",
                Email = "admin@fiapcloudgames.com",
                Password = "Admin@123456"
            });

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        var administrator = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal("Administrador Local", administrator.Name);
        Assert.Equal("admin@fiapcloudgames.com", administrator.Email.Value);
        Assert.Equal(UserRole.Administrator, administrator.Role);
        Assert.True(
            new PasswordHasher().Verify(
                "Admin@123456",
                administrator.PasswordHash));
        Assert.True(administrator.IsActive);
    }

    [Fact(DisplayName = "Semear administrador - deve falhar com configuração incompleta")]
    public async Task SeedAsync_WithIncompleteConfiguration_ShouldFail()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(
            context,
            new AdminSeedOptions
            {
                Email = "admin@fiapcloudgames.com"
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync(CancellationToken.None));

        Assert.Contains("Admin:Password", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.Users.ToListAsync());
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"admin-seed-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    private static AdminSeeder CreateSeeder(
        IdentityDbContext context,
        AdminSeedOptions options) =>
        new(
            context,
            new PasswordHasher(),
            Options.Create(options),
            new FixedTimeProvider(NowUtc),
            NullLogger<AdminSeeder>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
