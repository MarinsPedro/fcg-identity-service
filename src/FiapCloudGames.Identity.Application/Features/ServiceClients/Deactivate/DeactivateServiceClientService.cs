using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Deactivate;

public sealed class DeactivateServiceClientService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task ExecuteAsync(
        string clientId,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw AppException.NotFound("Client não encontrado.");
        if (string.Equals(client.ClientId, actorClientId, StringComparison.Ordinal))
        {
            throw AppException.Conflict("Um client não pode desativar a si próprio.");
        }

        try
        {
            client.Deactivate();
        }
        catch (DomainRuleViolationException exception)
        {
            throw AppException.Conflict(exception.Message);
        }

        await auditEvents.AddAsync(
            SecurityAuditEvent.Create(
                ClientAuditEvents.Deactivated,
                actorClientId,
                "service_client",
                client.ClientId,
                clock.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
