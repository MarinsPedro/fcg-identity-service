using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Migrations.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Database.IntegrationTests;

internal static class IdentityDbContextTestFactory
{
    private const string ConnectionString =
        "Host=localhost;Database=model_only;Username=model;Password=model";

    public static IdentityDbContext Create()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        MigrationDbContextOptions.Configure(options, ConnectionString);
        return new IdentityDbContext(options.Options);
    }
}
