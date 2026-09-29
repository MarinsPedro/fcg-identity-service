using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapCloudGames.Identity.Migrations;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var builder = MigrationConnection.CreateBuilder(args);
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        MigrationDbContextOptions.Configure(
            options,
            MigrationConnection.GetRequired(builder.Configuration));

        return new IdentityDbContext(options.Options);
    }
}
