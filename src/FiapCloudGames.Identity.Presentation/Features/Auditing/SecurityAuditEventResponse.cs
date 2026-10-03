using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

namespace FiapCloudGames.Identity.Presentation.Features.Auditing;

public sealed record SecurityAuditEventResponse(
    Guid Id,
    string EventType,
    string ActorClientId,
    string SubjectType,
    string SubjectId,
    DateTimeOffset OccurredAtUtc)
{
    public static SecurityAuditEventResponse From(SecurityAuditEventResult result) =>
        new(
            result.Id,
            result.EventType,
            result.ActorClientId,
            result.SubjectType,
            result.SubjectId,
            result.OccurredAtUtc);
}
