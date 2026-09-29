using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Migrations;
using Microsoft.Extensions.DependencyInjection;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    var builder = MigrationConnection.CreateBuilder(args);
    var connectionString = MigrationConnection.GetRequired(builder.Configuration);
    builder.Services.AddDbContext<IdentityDbContext>(options =>
        MigrationDbContextOptions.Configure(options, connectionString));
    builder.Services.AddScoped<MigrationRunner>();

    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    await runner.RunAsync(cancellation.Token);
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
    Console.Error.WriteLine(exception.Message);
    return 1;
}
