using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapCloudGames.Identity.Migrations;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                MigrationConnection.GetRequired(),
                postgres => postgres.MigrationsAssembly(
                    typeof(IdentityDbContextFactory).Assembly.GetName().Name))
            .Options;
        return new IdentityDbContext(options);
    }
}
