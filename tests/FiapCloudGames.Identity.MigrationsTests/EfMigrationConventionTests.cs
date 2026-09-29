using FiapCloudGames.Identity.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FiapCloudGames.Identity.MigrationsTests;

public sealed class EfMigrationConventionTests
{
    [Fact]
    public void Migrations_ShouldBeDiscoverable()
    {
        using var context = IdentityDbContextTestFactory.Create();

        Assert.NotEmpty(context.Database.GetMigrations());
    }

    [Fact]
    public void Model_ShouldHaveNoPendingChanges()
    {
        using var context = IdentityDbContextTestFactory.Create();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "O modelo do Identity diverge do snapshot de migrations.");
    }

    [Fact]
    public void DbContext_ShouldUseMigrationsProjectAssembly()
    {
        using var context = IdentityDbContextTestFactory.Create();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Equal(
            typeof(IdentityDbContextFactory).Assembly,
            migrationsAssembly.Assembly);
    }
}
