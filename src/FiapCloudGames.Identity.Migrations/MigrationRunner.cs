using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Migrations;

internal sealed class MigrationRunner(
    IdentityDbContext context,
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
                return;
            }

            logger.LogInformation(
                "Aplicando {MigrationCount} migration(s) do Identity: {Migrations}.",
                pending.Length,
                string.Join(", ", pending));
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation(
                "Migrations do Identity aplicadas com sucesso.");
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
