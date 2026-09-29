using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Api.Controllers;

[ApiController]
[Route("connect")]
public sealed class TokenController(ClientCredentialsService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType<ServiceTokenResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceTokenResult>> IssueAsync(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret,
        [FromForm(Name = "audience")] string audience,
        [FromForm(Name = "scope")] string scope,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            grantType,
            clientId,
            clientSecret,
            audience,
            scope,
            cancellationToken);
        return Ok(result);
    }
}
