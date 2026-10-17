using System.Security.Claims;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Create;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Deactivate;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Get;
using FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Controlador responsável pelo gerenciamento de service clients.
/// </summary>
[ApiController]
[Route("v1/service-clients")]
public sealed class ServiceClientsController : ControllerBase
{
    private const string GetByIdRoute = "Identity.ServiceClients.GetById";

    /// <summary>
    /// Cria um service client e retorna seu segredo uma única vez.
    /// </summary>
    /// <param name="request">Configuração inicial do service client.</param>
    /// <param name="service">Serviço responsável pela criação do client.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Client criado e segredo inicial.</returns>
    [Authorize(Policy = IdentityPolicies.ClientsCreate)]
    [HttpPost]
    [ProducesResponseType<CreateServiceClientResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status400BadRequest,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status409Conflict,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status415UnsupportedMediaType,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    public async Task<ActionResult<CreateServiceClientResponse>> CreateAsync(
        [FromBody] CreateServiceClientRequest request,
        [FromServices] CreateServiceClientService service,
        CancellationToken cancellationToken)
    {
        var response = (await service.ExecuteAsync(
            request.ToInput(),
            GetActorClientId(),
            cancellationToken)).ToResponse();
        return CreatedAtRoute(
            GetByIdRoute,
            new { clientId = response.Client.ClientId },
            response);
    }

    /// <summary>
    /// Obtém um service client pelo seu identificador.
    /// </summary>
    /// <param name="clientId">Identificador do service client.</param>
    /// <param name="service">Serviço responsável pela consulta do client.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados públicos e permissões do service client.</returns>
    [Authorize(Policy = IdentityPolicies.ClientsRead)]
    [HttpGet("{clientId}", Name = GetByIdRoute)]
    [ProducesResponseType<ServiceClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status404NotFound,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    public async Task<ActionResult<ServiceClientResponse>> GetAsync(
        string clientId,
        [FromServices] GetServiceClientService service,
        CancellationToken cancellationToken) =>
        Ok(ServiceClientResponse.From(
            await service.ExecuteAsync(clientId, cancellationToken)));

    /// <summary>
    /// Substitui as audiences e os scopes permitidos para um service client.
    /// </summary>
    /// <param name="clientId">Identificador do service client.</param>
    /// <param name="request">Novo conjunto completo de permissões.</param>
    /// <param name="service">Serviço responsável pela atualização das permissões.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados atualizados do service client.</returns>
    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpPut("{clientId}/permissions")]
    [ProducesResponseType<ServiceClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status400BadRequest,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status404NotFound,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status409Conflict,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status415UnsupportedMediaType,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
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

    /// <summary>
    /// Rotaciona o segredo de um service client.
    /// </summary>
    /// <param name="clientId">Identificador do service client.</param>
    /// <param name="service">Serviço responsável pela rotação do segredo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Novo segredo do service client, exibido uma única vez.</returns>
    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpPost("{clientId}/secret/rotate")]
    [ProducesResponseType<RotateServiceClientSecretResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status404NotFound,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status409Conflict,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    public async Task<ActionResult<RotateServiceClientSecretResponse>> RotateSecretAsync(
        string clientId,
        [FromServices] RotateServiceClientSecretService service,
        CancellationToken cancellationToken) =>
        Ok((await service.ExecuteAsync(
            clientId,
            GetActorClientId(),
            cancellationToken)).ToResponse());

    /// <summary>
    /// Desativa um service client sem removê-lo fisicamente.
    /// </summary>
    /// <param name="clientId">Identificador do service client.</param>
    /// <param name="service">Serviço responsável pela desativação do client.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação sem conteúdo.</returns>
    [Authorize(Policy = IdentityPolicies.ClientsWrite)]
    [HttpDelete("{clientId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status404NotFound,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status409Conflict,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError,
        FiapApiDefaultsExtensions.ProblemJsonContentType)]
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
