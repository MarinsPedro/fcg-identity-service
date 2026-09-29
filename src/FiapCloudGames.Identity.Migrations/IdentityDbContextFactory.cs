using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapCloudGames.Identity.Migrations;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = MigrationConnection.BuildConfiguration(args);
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                MigrationConnection.GetRequired(configuration),
                postgres => postgres.MigrationsAssembly(
                    typeof(IdentityDbContextFactory).Assembly.GetName().Name))
            .Options;
        return new IdentityDbContext(options);
    }
}
