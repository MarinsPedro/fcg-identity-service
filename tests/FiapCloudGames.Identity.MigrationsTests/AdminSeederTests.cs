using FiapCloudGames.Identity.Domain.Enums;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.MigrationsTests;

public sealed class AdminSeederTests
{
    [Fact]
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

    [Fact]
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
            TimeProvider.System,
            NullLogger<AdminSeeder>.Instance);
}
