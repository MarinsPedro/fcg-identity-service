using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] LoginService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(request.ToInput(), cancellationToken);
        return Ok(result.ToResponse());
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResponse>> RefreshAsync(
        [FromBody] RefreshRequest request,
        [FromServices] RefreshUserTokenService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(request.ToInput(), cancellationToken);
        return Ok(result.ToResponse());
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(
        [FromBody] RevokeRefreshTokenRequest request,
        [FromServices] RevokeRefreshTokenService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(request.ToInput(), cancellationToken);
        return NoContent();
    }
}
