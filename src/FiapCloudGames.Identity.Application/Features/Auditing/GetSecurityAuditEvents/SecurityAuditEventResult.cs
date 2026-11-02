using FiapCloudGames.Identity.Domain.Auditing;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

public sealed record SecurityAuditEventResult(
    Guid Id,
    string EventType,
    string ActorClientId,
    string SubjectType,
    string SubjectId,
    DateTimeOffset OccurredAtUtc)
{
    public static SecurityAuditEventResult From(SecurityAuditEvent auditEvent) =>
        new(
            auditEvent.Id,
            auditEvent.EventType,
            auditEvent.ActorClientId,
            auditEvent.SubjectType,
            auditEvent.SubjectId,
            auditEvent.OccurredAtUtc);
}
