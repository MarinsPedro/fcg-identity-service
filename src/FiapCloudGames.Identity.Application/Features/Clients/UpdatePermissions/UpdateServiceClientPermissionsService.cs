using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Clients.UpdatePermissions;

public sealed class UpdateServiceClientPermissionsService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<ServiceClientResult> ExecuteAsync(
        string clientId,
        UpdateServiceClientPermissionsInput input,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw AppException.NotFound("Client não encontrado.");
        if (!client.IsActive)
        {
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
        return ServiceClientResult.From(client);
    }
}
