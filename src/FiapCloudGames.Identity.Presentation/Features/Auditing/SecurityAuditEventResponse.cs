using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;

namespace FiapCloudGames.Identity.Presentation.Features.Auditing;

/// <summary>
/// Representa um evento administrativo de auditoria de segurança.
/// </summary>
/// <param name="Id">Identificador do evento.</param>
/// <param name="EventType">Tipo do evento registrado.</param>
/// <param name="ActorClientId">Service client responsável pela operação.</param>
/// <param name="SubjectType">Tipo do recurso afetado.</param>
/// <param name="SubjectId">Identificador do recurso afetado.</param>
/// <param name="OccurredAtUtc">Data e hora do evento em UTC.</param>
public sealed record SecurityAuditEventResponse(
    Guid Id,
    string EventType,
    string ActorClientId,
    string SubjectType,
    string SubjectId,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>
    /// Converte o resultado da aplicação em resposta HTTP.
    /// </summary>
    /// <param name="result">Evento retornado pela aplicação.</param>
    /// <returns>Resposta com os dados do evento de auditoria.</returns>
    public static SecurityAuditEventResponse From(SecurityAuditEventResult result) =>
        new(
            result.Id,
            result.EventType,
            result.ActorClientId,
            result.SubjectType,
            result.SubjectId,
            result.OccurredAtUtc);
}
