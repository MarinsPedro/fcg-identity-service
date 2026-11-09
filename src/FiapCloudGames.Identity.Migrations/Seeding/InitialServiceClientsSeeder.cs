using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class InitialServiceClientsSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<InitialServiceClientsOptions> optionsAccessor,
    TimeProvider clock,
    ILogger<InitialServiceClientsSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var configuredClients = optionsAccessor.Value.Clients;
        if (configuredClients.Count == 0)
        {
            throw new InvalidOperationException(
                "Configure ao menos um client em 'InitialServiceClients:Clients'.");
        }

        var clientIds = configuredClients
            .Select(client => client.ClientId?.Trim())
            .ToArray();
        if (clientIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "Todos os itens de 'InitialServiceClients:Clients' devem possuir ClientId.");
        }

        var duplicateClientId = clientIds
            .GroupBy(clientId => clientId!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateClientId is not null)
        {
            throw new InvalidOperationException(
                $"O client '{duplicateClientId}' está duplicado em 'InitialServiceClients:Clients'.");
        }

        var existingClients = await context.ServiceClients
            .Include(client => client.AllowedAudiences)
            .Include(client => client.AllowedScopes)
            .AsSplitQuery()
            .Where(client => clientIds.Contains(client.ClientId))
            .ToDictionaryAsync(client => client.ClientId, StringComparer.Ordinal, cancellationToken);

        var changed = false;
        foreach (var options in configuredClients)
        {
            var clientId = options.ClientId.Trim();
            ValidateSecret(options, clientId);
            if (existingClients.TryGetValue(clientId, out var existingClient))
            {
                changed |= SynchronizeExistingClient(existingClient, options);
                continue;
            }

            var now = clock.GetUtcNow();
            var client = ServiceClient.Create(
                clientId,
                options.DisplayName,
                passwordHasher.HashSecret(options.Secret!),
                options.CanIssueUserTokens,
                options.AllowedAudiences,
                options.AllowedScopes,
                now);
            await context.ServiceClients.AddAsync(client, cancellationToken);
            await AddAuditEventAsync(
                "service_client.provisioned",
                client.ClientId,
                now,
                cancellationToken);
            changed = true;
            logger.LogInformation(
                "Client inicial {ClientId} provisionado pelo migrador.",
                client.ClientId);
        }

        if (changed)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private bool SynchronizeExistingClient(
        ServiceClient client,
        InitialServiceClientOptions options)
    {
        var previousDisplayName = client.DisplayName;
        var previousCanIssueUserTokens = client.CanIssueUserTokens;
        var previousIsActive = client.IsActive;
        var previousAudiences = client.AllowedAudiences
            .Select(permission => permission.Audience)
            .ToHashSet(StringComparer.Ordinal);
        var previousScopes = client.AllowedScopes
            .Select(permission => permission.Scope)
            .ToHashSet(StringComparer.Ordinal);
        var secretMatches = passwordHasher.Verify(options.Secret!, client.SecretHash);

        client.ApplyProvisioningConfiguration(
            options.DisplayName,
            secretMatches
                ? client.SecretHash
                : passwordHasher.HashSecret(options.Secret!),
            options.CanIssueUserTokens,
            options.AllowedAudiences,
            options.AllowedScopes);

        var configurationChanged =
            !string.Equals(previousDisplayName, client.DisplayName, StringComparison.Ordinal) ||
            previousCanIssueUserTokens != client.CanIssueUserTokens ||
            previousIsActive != client.IsActive ||
            !secretMatches ||
            !previousAudiences.SetEquals(client.AllowedAudiences.Select(permission => permission.Audience)) ||
            !previousScopes.SetEquals(client.AllowedScopes.Select(permission => permission.Scope));

        if (!configurationChanged)
        {
            logger.LogInformation(
                "Client inicial {ClientId} já existe e sua configuração está sincronizada.",
                client.ClientId);
            return false;
        }

        context.SecurityAuditEvents.Add(SecurityAuditEvent.Create(
            "service_client.provisioning_synchronized",
            "identity-migrations",
            "service_client",
            client.ClientId,
            clock.GetUtcNow()));
        logger.LogInformation(
            "Configuração do client inicial {ClientId} sincronizada pelo migrador.",
            client.ClientId);
        return true;
    }

    private async Task AddAuditEventAsync(
        string eventType,
        string clientId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken) =>
        await context.SecurityAuditEvents.AddAsync(
            SecurityAuditEvent.Create(
                eventType,
                "identity-migrations",
                "service_client",
                clientId,
                occurredAtUtc),
            cancellationToken);

    private static void ValidateSecret(
        InitialServiceClientOptions options,
        string clientId)
    {
        if (string.IsNullOrWhiteSpace(options.Secret) ||
            string.Equals(options.Secret, "CHANGE_ME", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Configure o Secret de '{clientId}' em 'InitialServiceClients:Clients'.");
        }
    }
}
