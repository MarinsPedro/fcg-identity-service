namespace FiapCloudGames.Identity.Application.Abstractions.Auditing;

/// <summary>
/// Define os critérios persistentes da consulta de eventos de auditoria.
/// </summary>
/// <param name="EventType">Tipo exato do evento.</param>
/// <param name="ActorClientId">Identificador exato do service client autor.</param>
/// <param name="SubjectType">Tipo exato do recurso afetado.</param>
/// <param name="SubjectId">Identificador exato do recurso afetado.</param>
/// <param name="OccurredFromUtc">Início inclusivo do período.</param>
/// <param name="OccurredToUtc">Fim inclusivo do período.</param>
/// <param name="After">Posição exclusiva depois da qual a consulta continua.</param>
public sealed record SecurityAuditEventQuery(
    string? EventType,
    string? ActorClientId,
    string? SubjectType,
    string? SubjectId,
    DateTimeOffset? OccurredFromUtc,
    DateTimeOffset? OccurredToUtc,
    SecurityAuditEventCursor? After);
