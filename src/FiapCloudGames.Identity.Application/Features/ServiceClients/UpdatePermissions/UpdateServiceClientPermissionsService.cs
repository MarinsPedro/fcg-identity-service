using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;

public sealed class UpdateServiceClientPermissionsService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<UpdateServiceClientPermissionsService> logger)
{
    public async Task<ServiceClientResult> ExecuteAsync(
        string clientId,
        UpdateServiceClientPermissionsInput input,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando alteração das permissões do service client {ClientId} por {ActorClientId}.",
            clientId,
            actorClientId);

        var client = await clients.FindByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogInformation(
                "Não foi possível alterar permissões: service client {ClientId} não encontrado.",
                clientId);
            throw AppException.NotFound("Client não encontrado.");
        }

        if (!client.IsActive)
        {
            logger.LogInformation(
                "Não foi possível alterar permissões: service client {ClientId} está inativo.",
                clientId);
            throw AppException.Conflict("Não é possível alterar um client inativo.");
        }

        try
        {
            client.ReplacePermissions(
                input.AllowedAudiences,
                input.AllowedScopes);
        }
        catch (DomainRuleViolationException exception)
        {
            throw AppException.Validation(exception.Message);
        }

        await auditEvents.AddAsync(
            SecurityAuditEvent.Create(
                ClientAuditEvents.PermissionsChanged,
                actorClientId,
                "service_client",
                client.ClientId,
                clock.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Permissões do service client {ClientId} alteradas por {ActorClientId}.",
            client.ClientId,
            actorClientId);
        return ServiceClientResult.From(client);
    }
}
