using FiapCloudGames.Identity.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Discovery;

[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public sealed class DiscoveryController(ITokenDiscoveryProvider tokens) : ControllerBase
{
    [HttpGet("jwks.json")]
    [ProducesResponseType<JwksDocument>(StatusCodes.Status200OK)]
    public ActionResult<JwksDocument> Jwks() => Ok(tokens.GetJwks());

    [HttpGet("openid-configuration")]
    [ProducesResponseType<OpenIdConfigurationResponse>(StatusCodes.Status200OK)]
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
