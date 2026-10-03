using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Clients.RotateSecret;

public sealed class RotateServiceClientSecretService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    IClientSecretGenerator secretGenerator,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    public async Task<RotateServiceClientSecretResult> ExecuteAsync(
        string clientId,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw AppException.NotFound("Client não encontrado.");
        var rawSecret = secretGenerator.Generate();
        try
        {
            client.RotateSecret(passwordHasher.HashSecret(rawSecret));
        }
        catch (DomainRuleViolationException exception)
        {
            throw AppException.Conflict(exception.Message);
        }

        await auditEvents.AddAsync(
            SecurityAuditEvent.Create(
                ClientAuditEvents.SecretRotated,
                actorClientId,
                "service_client",
                client.ClientId,
                clock.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RotateServiceClientSecretResult(client.ClientId, rawSecret);
    }
}
