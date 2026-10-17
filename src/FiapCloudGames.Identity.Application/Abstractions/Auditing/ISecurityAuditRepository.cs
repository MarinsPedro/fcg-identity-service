using FiapCloudGames.Identity.Domain.Auditing;

namespace FiapCloudGames.Identity.Application.Abstractions.Auditing;

public interface ISecurityAuditRepository
{
    Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SecurityAuditEvent>> ListAsync(
        SecurityAuditEventQuery query,
        int limit,
        CancellationToken cancellationToken);
}
