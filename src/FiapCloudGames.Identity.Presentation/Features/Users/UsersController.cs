using FiapCloudGames.Identity.Application.Features.Users.CreateUser;
using FiapCloudGames.Identity.Application.Features.Users.DeactivateUser;
using FiapCloudGames.Identity.Application.Features.Users.GetUser;
using FiapCloudGames.Identity.Application.Features.Users.UpdateUser;
using FiapCloudGames.Identity.Presentation.Features.Users.CreateUser;
using FiapCloudGames.Identity.Presentation.Features.Users.UpdateUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

[ApiController]
[Route("internal/v1/users")]
public sealed class UsersController : ControllerBase
{
    [Authorize(Policy = IdentityPolicies.UsersCreate)]
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        [FromBody] CreateUserRequest request,
        [FromServices] CreateUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            request.ToInput(),
            cancellationToken);
        var response = result.ToResponse();
        return Created($"/internal/v1/users/{response.Id}", response);
    }

    [Authorize(Policy = IdentityPolicies.UsersRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> GetAsync(
        Guid id,
        [FromServices] GetUserService service,
        CancellationToken cancellationToken) =>
        Ok((await service.ExecuteAsync(id, cancellationToken)).ToResponse());

    [Authorize(Policy = IdentityPolicies.UsersWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        [FromServices] UpdateUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            id,
            request.ToInput(),
            cancellationToken);
        return Ok(result.ToResponse());
    }

    [Authorize(Policy = IdentityPolicies.UsersWrite)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAsync(
        Guid id,
        [FromServices] DeactivateUserService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
