using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Application.Pagination;
using FiapCloudGames.Identity.Presentation.Pagination;

namespace FiapCloudGames.Identity.Presentation.Features.Auditing;

/// <summary>
/// Fornece os mapeamentos HTTP para consulta da auditoria de segurança.
/// </summary>
internal static class SecurityAuditHttpMappings
{
    /// <summary>
    /// Converte filtros e cursor HTTP em entrada tipada da aplicação.
    /// </summary>
    /// <param name="request">Parâmetros recebidos na query string.</param>
    /// <returns>Entrada esperada pelo serviço de consulta.</returns>
    public static GetSecurityAuditEventsInput ToInput(
        this SecurityAuditEventsQueryRequest request)
    {
        SecurityAuditEventCursor? cursor = null;
        if (request.After is not null)
        {
            if (!CursorTokenCodec.TryDecode<SecurityAuditEventCursor>(
                    request.After,
                    out var decodedCursor))
            {
                throw AppException.Validation("O cursor informado é inválido.");
            }

            cursor = decodedCursor;
        }

        return new(
            request.Limit,
            cursor,
            request.EventType,
            request.ActorClientId,
            request.SubjectType,
            request.SubjectId,
            request.OccurredFromUtc,
            request.OccurredToUtc);
    }

    /// <summary>
    /// Converte a página tipada da aplicação em resposta HTTP com cursor opaco.
    /// </summary>
    /// <param name="result">Página retornada pela aplicação.</param>
    /// <returns>Página HTTP de eventos de auditoria.</returns>
    public static CursorPageResponse<SecurityAuditEventResponse> ToResponse(
        this CursorPageResult<SecurityAuditEventResult, SecurityAuditEventCursor> result) =>
        new(
            result.Items.Select(SecurityAuditEventResponse.From).ToArray(),
            result.NextCursor is { } cursor
                ? CursorTokenCodec.Encode(cursor)
                : null);
}
