using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Migrations;
using Microsoft.EntityFrameworkCore;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    var configuration = MigrationConnection.BuildConfiguration(args);
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
        .UseNpgsql(
            MigrationConnection.GetRequired(configuration),
            postgres => postgres.MigrationsAssembly(typeof(Program).Assembly.GetName().Name))
        .Options;

    await using var context = new IdentityDbContext(options);
    var pending = (await context.Database.GetPendingMigrationsAsync(cancellation.Token))
        .ToArray();

    if (pending.Length == 0)
    {
        Console.WriteLine("O banco do Identity já está atualizado.");
        return 0;
    }

    Console.WriteLine(
        "Aplicando {0} migration(s) do Identity: {1}.",
        pending.Length,
        string.Join(", ", pending));
    await context.Database.MigrateAsync(cancellation.Token);
    Console.WriteLine("Migrations do Identity aplicadas com sucesso.");
    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("A execução das migrations foi cancelada.");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Falha ao aplicar as migrations do Identity.");
    Console.Error.WriteLine(exception);
    return 1;
}
