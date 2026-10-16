using System.Security.Claims;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Controlador responsável pelas sessões de usuários autenticados.
/// </summary>
[ApiController]
[Route("v1/auth")]
public sealed class AuthenticationController : ControllerBase
{
    /// <summary>
    /// Autentica um usuário por meio do service client solicitante.
    /// </summary>
    /// <param name="request">Credenciais do usuário.</param>
    /// <param name="service">Serviço responsável pela autenticação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Par de tokens e dados do usuário autenticado.</returns>
    [Authorize(Policy = IdentityPolicies.AuthLogin)]
    [HttpPost("login")]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] LoginService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            request.ToInput(GetClientId()),
            cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Renova a sessão do usuário e rotaciona o refresh token.
    /// </summary>
    /// <param name="request">Refresh token atual.</param>
    /// <param name="service">Serviço responsável pela renovação da sessão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Novo par de tokens e dados do usuário.</returns>
    [Authorize(Policy = IdentityPolicies.AuthRefresh)]
    [HttpPost("refresh")]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenPairResponse>> RefreshAsync(
        [FromBody] RefreshRequest request,
        [FromServices] RefreshUserTokenService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            request.ToInput(GetClientId()),
            cancellationToken);
        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Revoga o refresh token usado pela sessão do usuário.
    /// </summary>
    /// <param name="request">Refresh token que será revogado.</param>
    /// <param name="service">Serviço responsável pela revogação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação sem conteúdo.</returns>
    [Authorize(Policy = IdentityPolicies.AuthRevoke)]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(
        [FromBody] RevokeRefreshTokenRequest request,
        [FromServices] RevokeRefreshTokenService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(
            request.ToInput(GetClientId()),
            cancellationToken);
        return NoContent();
    }

    private string GetClientId() =>
        User.FindFirstValue(FiapClaimTypes.ClientId)
        ?? throw new InvalidOperationException(
            "O token autenticado não contém client_id.");
}
