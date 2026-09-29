using FiapCloudGames.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public sealed class DiscoveryController(
    IJwksProvider keys,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpGet("jwks.json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Jwks() => Ok(keys.GetDocument());

    [HttpGet("openid-configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult OpenIdConfiguration()
    {
        var authority = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            issuer = jwtOptions.Value.Issuer,
            token_endpoint = $"{authority}/connect/token",
            jwks_uri = $"{authority}/.well-known/jwks.json",
            grant_types_supported = new[] { "client_credentials" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }
}
