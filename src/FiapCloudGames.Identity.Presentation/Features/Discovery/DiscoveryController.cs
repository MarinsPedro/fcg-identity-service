using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Discovery;

/// <summary>
/// Controlador responsável pelos documentos de descoberta e pelas chaves públicas.
/// </summary>
/// <param name="tokens">Provedor dos metadados e das chaves de assinatura.</param>
[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public sealed class DiscoveryController(ITokenDiscoveryProvider tokens) : ControllerBase
{
    /// <summary>
    /// Obtém o documento JWKS com as chaves públicas usadas na validação dos JWTs.
    /// </summary>
    /// <returns>Documento JSON Web Key Set do emissor.</returns>
    [HttpGet("jwks.json")]
    [ProducesResponseType<JwksResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    public ActionResult<JwksResponse> Jwks() =>
        Ok(JwksResponse.From(tokens.GetJwks()));

    /// <summary>
    /// Obtém os metadados OpenID Connect publicados pelo Identity.
    /// </summary>
    /// <returns>Metadados do emissor, do token endpoint e do JWKS.</returns>
    [HttpGet("openid-configuration")]
    [ProducesResponseType<OpenIdConfigurationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    public ActionResult<OpenIdConfigurationResponse> OpenIdConfiguration()
    {
        var authority = $"{Request.Scheme}://{Request.Host}";
        return Ok(new OpenIdConfigurationResponse(
            tokens.Issuer,
            $"{authority}/oauth2/token",
            $"{authority}/.well-known/jwks.json",
            ["client_credentials"],
            ["client_secret_post"],
            ["RS256"]));
    }
}
