using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

[ApiController]
[Route("connect")]
public sealed class TokenController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType<ServiceTokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceTokenResponse>> IssueAsync(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret,
        [FromForm(Name = "audience")] string audience,
        [FromForm(Name = "scope")] string scope,
        [FromServices] ClientCredentialsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            new ClientCredentialsInput(
                grantType,
                clientId,
                clientSecret,
                audience,
                scope),
            cancellationToken);
        return Ok(result.ToResponse());
    }
}
