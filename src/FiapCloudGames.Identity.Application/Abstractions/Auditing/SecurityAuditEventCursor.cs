namespace FiapCloudGames.Identity.Application.Abstractions.Auditing;

/// <summary>
/// Identifica uma posição estável na ordenação dos eventos de auditoria.
/// </summary>
/// <param name="OccurredAtUtc">Data do último evento retornado.</param>
/// <param name="Id">Identificador usado como critério de desempate.</param>
public readonly record struct SecurityAuditEventCursor(
    DateTimeOffset OccurredAtUtc,
    Guid Id);
