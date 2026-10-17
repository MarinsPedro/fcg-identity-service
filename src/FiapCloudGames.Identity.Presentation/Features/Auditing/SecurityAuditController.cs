using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Presentation.Authorization;
using FiapCloudGames.Identity.Presentation.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Auditing;

/// <summary>
/// Controlador responsável pela consulta de eventos de auditoria de segurança.
/// </summary>
[ApiController]
[Route("v1/audit-events")]
public sealed class SecurityAuditController : ControllerBase
{
    /// <summary>
    /// Lista uma página de eventos de auditoria de segurança.
    /// </summary>
    /// <param name="request">Filtros, limite e cursor da consulta.</param>
    /// <param name="service">Serviço responsável pela consulta dos eventos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de eventos e cursor para continuar a consulta.</returns>
    [Authorize(Policy = IdentityPolicies.AuditRead)]
    [HttpGet]
    [ProducesResponseType<CursorPageResponse<SecurityAuditEventResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPageResponse<SecurityAuditEventResponse>>> GetAsync(
        [FromQuery] SecurityAuditEventsQueryRequest request,
        [FromServices] GetSecurityAuditEventsService service,
        CancellationToken cancellationToken)
    {
        var page = await service.ExecuteAsync(request.ToInput(), cancellationToken);
        return Ok(page.ToResponse());
    }
}
