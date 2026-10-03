using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Migrations;

internal sealed class MigrationRunner(
    IdentityDbContext context,
    AdminSeeder adminSeeder,
    ILogger<MigrationRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            var pending = (await context.Database
                .GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            if (pending.Length == 0)
            {
                logger.LogInformation("O banco do Identity já está atualizado.");
            }
            else
            {
                logger.LogInformation(
                    "Aplicando {MigrationCount} migration(s) do Identity: {Migrations}.",
                    pending.Length,
                    string.Join(", ", pending));
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation(
                    "Migrations do Identity aplicadas com sucesso.");
            }

            await adminSeeder.SeedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("A execução das migrations foi cancelada.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Falha ao aplicar as migrations do Identity.");
            throw;
        }
    }
}
