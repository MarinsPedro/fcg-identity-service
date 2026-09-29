using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Api.Controllers;

[ApiController]
[Route("internal/users")]
public sealed class UsersController(
    RegisterUserService registerService,
    GetUserService getService,
    UpdateUserService updateService,
    DeactivateUserService deactivateService) : ControllerBase
{
    [Authorize(Policy = IdentityPolicies.UsersCreate)]
    [HttpPost]
    [ProducesResponseType<UserResult>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResult>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerService.ExecuteAsync(
            request.Name,
            request.Email,
            request.Password,
            cancellationToken);
        return Created($"/internal/users/{result.Id}", result);
    }

    [Authorize(Policy = IdentityPolicies.UsersRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResult>> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await getService.ExecuteAsync(id, cancellationToken));

    [Authorize(Policy = IdentityPolicies.UsersWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResult>> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateService.ExecuteAsync(
            id,
            request.Name,
            request.Email,
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = IdentityPolicies.UsersWrite)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await deactivateService.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
