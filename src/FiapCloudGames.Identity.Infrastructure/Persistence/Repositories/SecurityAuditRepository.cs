using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;

internal sealed class SecurityAuditRepository(IdentityDbContext context)
    : ISecurityAuditRepository
{
    public Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken) =>
        context.SecurityAuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<SecurityAuditEvent>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken) =>
        await context.SecurityAuditEvents
            .AsNoTracking()
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Take(take)
            .ToArrayAsync(cancellationToken);
}
