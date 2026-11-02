using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Security;
using FiapCloudGames.Identity.Migrations.Configuration;
using FiapCloudGames.Identity.Migrations.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Migrations;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
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
            builder.Services.Configure<AdminSeedOptions>(
                builder.Configuration.GetSection(AdminSeedOptions.SectionName));
            builder.Services.Configure<InitialServiceClientOptions>(
                builder.Configuration.GetSection(InitialServiceClientOptions.SectionName));
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<AdminSeeder>();
            builder.Services.AddScoped<InitialServiceClientSeeder>();
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
    }
}
