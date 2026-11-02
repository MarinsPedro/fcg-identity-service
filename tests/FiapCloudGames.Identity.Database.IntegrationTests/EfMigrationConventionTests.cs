using FiapCloudGames.Identity.Migrations.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FiapCloudGames.Identity.Database.IntegrationTests;

public sealed class EfMigrationConventionTests
{
    [Fact(DisplayName = "Obter migrations - deve encontrar migrations registradas")]
    public void Migrations_ShouldBeDiscoverable()
    {
        using var context = IdentityDbContextTestFactory.Create();

        Assert.NotEmpty(context.Database.GetMigrations());
    }

    [Fact(DisplayName = "Obter modelo - não deve ter alterações pendentes")]
    public void Model_ShouldHaveNoPendingChanges()
    {
        using var context = IdentityDbContextTestFactory.Create();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "O modelo do Identity diverge do snapshot de migrations.");
    }

    [Fact(DisplayName = "Configurar DbContext - deve usar o assembly do projeto de migrations")]
    public void DbContext_ShouldUseMigrationsProjectAssembly()
    {
        using var context = IdentityDbContextTestFactory.Create();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Equal(
            typeof(IdentityDbContextFactory).Assembly,
            migrationsAssembly.Assembly);
    }

}
