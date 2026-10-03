using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;
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

        if (await context.ServiceClients.AnyAsync(
            client => client.ClientId == InitialServiceClientOptions.RequiredClientId,
            cancellationToken))
        {
            logger.LogInformation(
                "Client inicial {ClientId} já existe; seed ignorado.",
                InitialServiceClientOptions.RequiredClientId);
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
}
