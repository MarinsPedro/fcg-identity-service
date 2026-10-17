using FiapCloudGames.Identity.Application.Abstractions.Auditing;

namespace FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

/// <summary>
/// Define filtros e paginação da consulta de eventos de auditoria.
/// </summary>
/// <param name="Limit">Quantidade máxima de itens da página.</param>
/// <param name="After">Cursor da página anterior.</param>
/// <param name="EventType">Tipo exato do evento.</param>
/// <param name="ActorClientId">Service client autor da operação.</param>
/// <param name="SubjectType">Tipo do recurso afetado.</param>
/// <param name="SubjectId">Identificador do recurso afetado.</param>
/// <param name="OccurredFromUtc">Início inclusivo do período, em UTC.</param>
/// <param name="OccurredToUtc">Fim inclusivo do período, em UTC.</param>
public sealed record GetSecurityAuditEventsInput(
    int Limit,
    SecurityAuditEventCursor? After,
    string? EventType,
    string? ActorClientId,
    string? SubjectType,
    string? SubjectId,
    DateTimeOffset? OccurredFromUtc,
    DateTimeOffset? OccurredToUtc);
