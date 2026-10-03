using FiapCloudGames.Identity.Migrations;
using FiapCloudGames.Identity.Migrations.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

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

    [Fact]
    public void PermissionNormalization_ShouldCopyDataBeforeDroppingOldColumns()
    {
        var operations = new NormalizeServiceClientPermissions().UpOperations;
        var copyDataIndex = operations
            .Select((operation, index) => (operation, index))
            .Single(item => item.operation is SqlOperation)
            .index;
        var firstDropIndex = operations
            .Select((operation, index) => (operation, index))
            .Where(item => item.operation is DropColumnOperation)
            .Min(item => item.index);

        Assert.True(copyDataIndex < firstDropIndex);
        Assert.Equal(
            ["allowed_audiences", "allowed_scopes"],
            operations
                .OfType<DropColumnOperation>()
                .Select(operation => operation.Name)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void PermissionNormalizationRollback_ShouldRestoreDataBeforeDroppingTables()
    {
        var operations = new NormalizeServiceClientPermissions().DownOperations;
        var restoreDataIndex = operations
            .Select((operation, index) => (operation, index))
            .Single(item => item.operation is SqlOperation)
            .index;
        var lastAddIndex = operations
            .Select((operation, index) => (operation, index))
            .Where(item => item.operation is AddColumnOperation)
            .Max(item => item.index);
        var firstDropIndex = operations
            .Select((operation, index) => (operation, index))
            .Where(item => item.operation is DropTableOperation)
            .Min(item => item.index);

        Assert.True(lastAddIndex < restoreDataIndex);
        Assert.True(restoreDataIndex < firstDropIndex);
    }
}
