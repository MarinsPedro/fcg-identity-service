using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.IntegrationTests.Support;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("_tests")]
public sealed class TransversalTestController : ControllerBase
{
    [HttpGet("unexpected-error")]
    public IActionResult ThrowUnexpectedError() =>
        throw new InvalidOperationException("sensitive-internal-message");
}
