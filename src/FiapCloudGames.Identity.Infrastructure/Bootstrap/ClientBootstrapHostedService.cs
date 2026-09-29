using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Infrastructure.Bootstrap;

internal sealed class ClientBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityBootstrapOptions> optionsAccessor,
    TimeProvider clock,
    ILogger<ClientBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var clients = optionsAccessor.Value.BootstrapClients;
        if (clients.Count == 0)
        {
            logger.LogWarning("Nenhum client de bootstrap foi configurado.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdentityStore>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var changed = false;

        foreach (var options in clients)
        {
            if (string.IsNullOrWhiteSpace(options.Secret) ||
                string.Equals(options.Secret, "CHANGE_ME", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Configure um segredo real para o client '{options.ClientId}' por variável de ambiente ou user-secrets.");
            }

            if (await store.FindClientAsync(options.ClientId, cancellationToken) is not null)
            {
                continue;
            }

            var client = ServiceClient.Create(
                options.ClientId,
                options.DisplayName,
                hasher.HashSecret(options.Secret),
                options.CanIssueUserTokens,
                options.AllowedAudiences,
                options.AllowedScopes,
                clock.GetUtcNow());
            await store.AddClientAsync(client, cancellationToken);
            changed = true;
            logger.LogInformation("Client {ClientId} criado pelo bootstrap.", client.ClientId);
        }

        if (changed)
        {
            await store.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
