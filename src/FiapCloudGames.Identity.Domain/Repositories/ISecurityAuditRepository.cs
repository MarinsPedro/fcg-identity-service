using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Domain.Repositories;

public interface ISecurityAuditRepository
{
    Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SecurityAuditEvent>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken);
}
