using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    LoginService loginService,
    RefreshUserTokenService refreshService,
    RevokeRefreshTokenService revokeService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<TokenPairResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResult>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginService.ExecuteAsync(
            request.Email,
            request.Password,
            request.ClientId,
            request.ClientSecret,
            cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenPairResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResult>> RefreshAsync(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshService.ExecuteAsync(
            request.RefreshToken,
            request.ClientId,
            request.ClientSecret,
            cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(
        [FromBody] RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await revokeService.ExecuteAsync(
            request.RefreshToken,
            request.ClientId,
            request.ClientSecret,
            cancellationToken);
        return NoContent();
    }
}
