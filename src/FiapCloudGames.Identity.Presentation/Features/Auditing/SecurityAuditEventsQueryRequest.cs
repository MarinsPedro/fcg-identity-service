using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Presentation.Pagination;
using FiapCloudGames.Identity.Presentation.Validation;

namespace FiapCloudGames.Identity.Presentation.Features.Auditing;

/// <summary>
/// Define os filtros HTTP da consulta de eventos de auditoria.
/// </summary>
public sealed class SecurityAuditEventsQueryRequest : CursorQueryRequest
{
    /// <summary>
    /// Tipo exato do evento.
    /// </summary>
    [ApiStringLength(SecurityAuditEventQueryConstraints.EventTypeMaximumLength)]
    public string? EventType { get; init; }

    /// <summary>
    /// Identificador exato do service client autor da operação.
    /// </summary>
    [ApiStringLength(SecurityAuditEventQueryConstraints.ActorClientIdMaximumLength)]
    public string? ActorClientId { get; init; }

    /// <summary>
    /// Tipo exato do recurso afetado.
    /// </summary>
    [ApiStringLength(SecurityAuditEventQueryConstraints.SubjectTypeMaximumLength)]
    public string? SubjectType { get; init; }

    /// <summary>
    /// Identificador exato do recurso afetado.
    /// </summary>
    [ApiStringLength(SecurityAuditEventQueryConstraints.SubjectIdMaximumLength)]
    public string? SubjectId { get; init; }

    /// <summary>
    /// Início inclusivo do período, em UTC.
    /// </summary>
    public DateTimeOffset? OccurredFromUtc { get; init; }

    /// <summary>
    /// Fim inclusivo do período, em UTC.
    /// </summary>
    public DateTimeOffset? OccurredToUtc { get; init; }
}
