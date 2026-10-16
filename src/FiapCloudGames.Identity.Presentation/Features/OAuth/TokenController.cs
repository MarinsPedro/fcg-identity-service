using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.OAuth;

/// <summary>
/// Controlador responsável pela emissão de tokens OAuth para service clients.
/// </summary>
[ApiController]
[Route("oauth2")]
public sealed class TokenController : ControllerBase
{
    /// <summary>
    /// Emite um token de serviço pelo fluxo client credentials.
    /// </summary>
    /// <param name="grantType">Tipo de concessão OAuth; deve ser <c>client_credentials</c>.</param>
    /// <param name="clientId">Identificador do service client.</param>
    /// <param name="clientSecret">Segredo do service client.</param>
    /// <param name="audience">API destinatária do token.</param>
    /// <param name="scope">Scopes solicitados, separados por espaço.</param>
    /// <param name="service">Serviço responsável pela autenticação do client e emissão do token.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Token de acesso técnico emitido para o service client.</returns>
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
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(result.ToResponse());
    }
}
