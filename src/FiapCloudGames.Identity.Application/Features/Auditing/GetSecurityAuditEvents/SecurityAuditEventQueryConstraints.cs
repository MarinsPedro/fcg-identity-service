using FiapCloudGames.Identity.Domain.Auditing;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

/// <summary>
/// Expõe à borda os limites aceitos pelos filtros de auditoria.
/// </summary>
public static class SecurityAuditEventQueryConstraints
{
    public const int EventTypeMaximumLength =
        SecurityAuditEvent.EventTypeMaximumLength;
    public const int ActorClientIdMaximumLength =
        SecurityAuditEvent.ActorClientIdMaximumLength;
    public const int SubjectTypeMaximumLength =
        SecurityAuditEvent.SubjectTypeMaximumLength;
    public const int SubjectIdMaximumLength =
        SecurityAuditEvent.SubjectIdMaximumLength;
}
