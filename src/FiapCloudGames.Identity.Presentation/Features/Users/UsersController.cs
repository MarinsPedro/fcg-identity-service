using FiapCloudGames.Identity.Application.Features.Users.Create;
using FiapCloudGames.Identity.Application.Features.Users.Deactivate;
using FiapCloudGames.Identity.Application.Features.Users.Get;
using FiapCloudGames.Identity.Application.Features.Users.Update;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

[ApiController]
[Route("internal/v1/users")]
public sealed class UsersController : ControllerBase
{
    private const string GetByIdRoute = "Identity.Users.GetById";

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
        return CreatedAtRoute(
            GetByIdRoute,
            new { id = response.Id },
            response);
    }

    [Authorize(Policy = IdentityPolicies.UsersRead)]
    [HttpGet("{id:guid}", Name = GetByIdRoute)]
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
