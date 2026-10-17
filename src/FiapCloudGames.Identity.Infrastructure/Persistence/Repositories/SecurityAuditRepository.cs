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

    public async Task<IReadOnlyList<SecurityAuditEvent>> ListAsync(
        SecurityAuditEventQuery query,
        int limit,
        CancellationToken cancellationToken)
    {
        var auditEvents = BuildQuery(context, query);

        return await auditEvents
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    internal static IQueryable<SecurityAuditEvent> BuildQuery(
        IdentityDbContext context,
        SecurityAuditEventQuery query)
    {
        var auditEvents = context.SecurityAuditEvents.AsNoTracking();

        if (query.EventType is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.EventType == query.EventType);
        }

        if (query.ActorClientId is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.ActorClientId == query.ActorClientId);
        }

        if (query.SubjectType is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.SubjectType == query.SubjectType);
        }

        if (query.SubjectId is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.SubjectId == query.SubjectId);
        }

        if (query.OccurredFromUtc is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.OccurredAtUtc >= query.OccurredFromUtc.Value);
        }

        if (query.OccurredToUtc is not null)
        {
            auditEvents = auditEvents.Where(
                auditEvent => auditEvent.OccurredAtUtc <= query.OccurredToUtc.Value);
        }

        if (query.After is { } cursor)
        {
            auditEvents = context.Database.IsNpgsql()
                ? auditEvents.Where(auditEvent => EF.Functions.LessThan(
                    new ValueTuple<DateTimeOffset, Guid>(
                        auditEvent.OccurredAtUtc,
                        auditEvent.Id),
                    new ValueTuple<DateTimeOffset, Guid>(
                        cursor.OccurredAtUtc,
                        cursor.Id)))
                : auditEvents.Where(auditEvent =>
                    auditEvent.OccurredAtUtc < cursor.OccurredAtUtc ||
                    auditEvent.OccurredAtUtc == cursor.OccurredAtUtc &&
                    auditEvent.Id.CompareTo(cursor.Id) < 0);
        }

        return auditEvents;
    }
}
