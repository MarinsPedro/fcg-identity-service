using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;

public sealed class RotateServiceClientSecretService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    IClientSecretGenerator secretGenerator,
    IPasswordHasher passwordHasher,
    TimeProvider clock,
    ILogger<RotateServiceClientSecretService> logger)
{
    public async Task<RotateServiceClientSecretResult> ExecuteAsync(
        string clientId,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando rotação do segredo do service client {ClientId} por {ActorClientId}.",
            clientId,
            actorClientId);

        var client = await clients.FindByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogInformation(
                "Não foi possível rotacionar o segredo: service client {ClientId} não encontrado.",
                clientId);
            throw AppException.NotFound("Client não encontrado.");
        }

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
        logger.LogInformation(
            "Segredo do service client {ClientId} rotacionado por {ActorClientId}.",
            client.ClientId,
            actorClientId);
        return new RotateServiceClientSecretResult(client.ClientId, rawSecret);
    }
}
