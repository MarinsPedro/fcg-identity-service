using System.Security.Claims;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Application.Features.Clients.CreateClient;
using FiapCloudGames.Identity.Application.Features.Clients.DeactivateClient;
using FiapCloudGames.Identity.Application.Features.Clients.GetClient;
using FiapCloudGames.Identity.Application.Features.Clients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.Clients.UpdatePermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.Clients;

[ApiController]
[Route("internal/v1/service-clients")]
public sealed class ServiceClientsController : ControllerBase
{
    [Authorize(Policy = IdentityPolicies.ClientsCreate)]
    [HttpPost]
    [ProducesResponseType<CreateServiceClientResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateServiceClientResponse>> CreateAsync(
        [FromBody] CreateServiceClientRequest request,
        [FromServices] CreateServiceClientService service,
        CancellationToken cancellationToken)
    {
        var response = (await service.ExecuteAsync(
            request.ToInput(),
            GetActorClientId(),
            cancellationToken)).ToResponse();
        return Created(
            $"/internal/v1/service-clients/{response.Client.ClientId}",
            response);
    }

    [Authorize(Policy = IdentityPolicies.ClientsRead)]
    [HttpGet("{clientId}")]
    [ProducesResponseType<ServiceClientResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceClientResponse>> GetAsync(
        string clientId,
        [FromServices] GetServiceClientService service,
        CancellationToken cancellationToken) =>
        Ok(ServiceClientResponse.From(
            await service.ExecuteAsync(clientId, cancellationToken)));

    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpPut("{clientId}/permissions")]
    [ProducesResponseType<ServiceClientResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceClientResponse>> UpdatePermissionsAsync(
        string clientId,
        [FromBody] UpdateServiceClientPermissionsRequest request,
        [FromServices] UpdateServiceClientPermissionsService service,
        CancellationToken cancellationToken) =>
        Ok(ServiceClientResponse.From(
            await service.ExecuteAsync(
                clientId,
                request.ToInput(),
                GetActorClientId(),
                cancellationToken)));

    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpPost("{clientId}/secret/rotate")]
    [ProducesResponseType<RotateServiceClientSecretResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RotateServiceClientSecretResponse>> RotateSecretAsync(
        string clientId,
        [FromServices] RotateServiceClientSecretService service,
        CancellationToken cancellationToken) =>
        Ok((await service.ExecuteAsync(
            clientId,
            GetActorClientId(),
            cancellationToken)).ToResponse());

    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpDelete("{clientId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAsync(
        string clientId,
        [FromServices] DeactivateServiceClientService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(
            clientId,
            GetActorClientId(),
            cancellationToken);
        return NoContent();
    }

    private string GetActorClientId() =>
        User.FindFirstValue(FiapClaimTypes.ClientId)
        ?? throw new InvalidOperationException(
            "O token autenticado não contém client_id.");
}
