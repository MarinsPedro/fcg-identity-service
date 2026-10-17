using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class InitialServiceClientSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<InitialServiceClientOptions> optionsAccessor,
    TimeProvider clock,
    ILogger<InitialServiceClientSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        if (!string.Equals(
            options.ClientId?.Trim(),
            InitialServiceClientOptions.RequiredClientId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "O único client inicial permitido é 'web-bff'.");
        }

        var existingClient = await context.ServiceClients
            .Include(client => client.AllowedAudiences)
            .Include(client => client.AllowedScopes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                client => client.ClientId == InitialServiceClientOptions.RequiredClientId,
                cancellationToken);
        if (existingClient is not null)
        {
            await AddMissingPermissionsAsync(
                existingClient,
                options,
                cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Secret) ||
            string.Equals(options.Secret, "CHANGE_ME", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Configure 'InitialServiceClient:Secret' para provisionar o web-bff.");
        }

        var now = clock.GetUtcNow();
        var client = ServiceClient.Create(
            InitialServiceClientOptions.RequiredClientId,
            options.DisplayName,
            passwordHasher.HashSecret(options.Secret),
            options.CanIssueUserTokens,
            options.AllowedAudiences,
            options.AllowedScopes,
            now);
        var auditEvent = SecurityAuditEvent.Create(
            "service_client.provisioned",
            "identity-migrations",
            "service_client",
            client.ClientId,
            now);

        await context.ServiceClients.AddAsync(client, cancellationToken);
        await context.SecurityAuditEvents.AddAsync(auditEvent, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Client inicial {ClientId} provisionado pelo migrador.",
            client.ClientId);
    }

    private async Task AddMissingPermissionsAsync(
        ServiceClient client,
        InitialServiceClientOptions options,
        CancellationToken cancellationToken)
    {
        var audienceCount = client.AllowedAudiences.Count;
        var scopeCount = client.AllowedScopes.Count;

        foreach (var audience in options.AllowedAudiences)
        {
            client.AllowAudience(audience);
        }

        foreach (var scope in options.AllowedScopes)
        {
            client.GrantScope(scope);
        }

        var addedAudienceCount = client.AllowedAudiences.Count - audienceCount;
        var addedScopeCount = client.AllowedScopes.Count - scopeCount;
        if (addedAudienceCount == 0 && addedScopeCount == 0)
        {
            logger.LogInformation(
                "Client inicial {ClientId} já existe e suas permissões estão atualizadas.",
                client.ClientId);
            return;
        }

        var auditEvent = SecurityAuditEvent.Create(
            "service_client.permissions_changed",
            "identity-migrations",
            "service_client",
            client.ClientId,
            clock.GetUtcNow());

        await context.SecurityAuditEvents.AddAsync(auditEvent, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seed adicionou {AudienceCount} audience(s) e {ScopeCount} scope(s) " +
            "ao client inicial {ClientId}.",
            addedAudienceCount,
            addedScopeCount,
            client.ClientId);
    }
}
