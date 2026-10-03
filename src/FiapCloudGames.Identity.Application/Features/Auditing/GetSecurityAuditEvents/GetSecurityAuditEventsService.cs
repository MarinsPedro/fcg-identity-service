using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

public sealed class GetSecurityAuditEventsService(
    ISecurityAuditRepository auditEvents)
{
    public async Task<IReadOnlyCollection<SecurityAuditEventResult>> ExecuteAsync(
        int take,
        CancellationToken cancellationToken)
    {
        if (take is < 1 or > 200)
        {
            throw AppException.Validation("O limite deve estar entre 1 e 200.");
        }

        var events = await auditEvents.GetRecentAsync(take, cancellationToken);
        return events.Select(SecurityAuditEventResult.From).ToArray();
    }
}
