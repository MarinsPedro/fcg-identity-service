using FiapCloudGames.Identity.Domain.Auditing;

namespace FiapCloudGames.Identity.Application.Abstractions.Auditing;

public interface ISecurityAuditRepository
{
    Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SecurityAuditEvent>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken);
}
