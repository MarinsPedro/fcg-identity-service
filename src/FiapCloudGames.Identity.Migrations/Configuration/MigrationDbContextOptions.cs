using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Migrations.Configuration;

internal static class MigrationDbContextOptions
{
    private static readonly string MigrationsAssemblyName =
        typeof(MigrationDbContextOptions).Assembly.GetName().Name!;

    public static void Configure(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseNpgsql(
            connectionString,
            postgres =>
            {
                postgres.MigrationsAssembly(MigrationsAssemblyName);
                postgres.CommandTimeout(120);
                postgres.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
    }
}
