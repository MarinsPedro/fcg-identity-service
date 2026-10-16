using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Presentation.Authorization;
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
    /// Lista os eventos de auditoria de segurança mais recentes.
    /// </summary>
    /// <param name="take">Quantidade máxima de eventos; o padrão é 100.</param>
    /// <param name="service">Serviço responsável pela consulta dos eventos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção dos eventos de auditoria encontrados.</returns>
    [Authorize(Policy = IdentityPolicies.AuditRead)]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<SecurityAuditEventResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SecurityAuditEventResponse>>> GetAsync(
        [FromQuery] int take,
        [FromServices] GetSecurityAuditEventsService service,
        CancellationToken cancellationToken)
    {
        var normalizedTake = take == 0 ? 100 : take;
        var events = await service.ExecuteAsync(normalizedTake, cancellationToken);
        return Ok(events.Select(SecurityAuditEventResponse.From).ToArray());
    }
}
