using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

public sealed class GetSecurityAuditEventsService(
    ISecurityAuditRepository auditEvents,
    ILogger<GetSecurityAuditEventsService> logger)
{
    public async Task<IReadOnlyCollection<SecurityAuditEventResult>> ExecuteAsync(
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Consultando os {AuditEventLimit} eventos de auditoria mais recentes.",
            take);

        if (take is < 1 or > 200)
        {
            throw AppException.Validation("O limite deve estar entre 1 e 200.");
        }

        var events = await auditEvents.GetRecentAsync(take, cancellationToken);
        logger.LogDebug(
            "Consulta de auditoria concluída com {AuditEventCount} evento(s).",
            events.Count);
        return events.Select(SecurityAuditEventResult.From).ToArray();
    }
}
