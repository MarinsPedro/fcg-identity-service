using FiapCloudGames.Identity.Application.Features.Audit.GetSecurityAuditEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Audit;

[ApiController]
[Route("internal/v1/audit-events")]
public sealed class SecurityAuditController : ControllerBase
{
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
