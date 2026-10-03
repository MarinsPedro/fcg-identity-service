using FiapCloudGames.Identity.Application.Features.Clients.CreateClient;
using FiapCloudGames.Identity.Application.Features.Clients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.Clients.UpdatePermissions;

namespace FiapCloudGames.Identity.Presentation.Features.Clients;

internal static class ServiceClientHttpMappings
{
    public static CreateServiceClientInput ToInput(
        this CreateServiceClientRequest request) =>
        new(
            request.ClientId,
            request.DisplayName,
            request.CanIssueUserTokens,
            request.AllowedAudiences,
            request.AllowedScopes);

    public static UpdateServiceClientPermissionsInput ToInput(
        this UpdateServiceClientPermissionsRequest request) =>
        new(request.AllowedAudiences, request.AllowedScopes);

    public static CreateServiceClientResponse ToResponse(
        this CreateServiceClientResult result) =>
        new(ServiceClientResponse.From(result.Client), result.ClientSecret);

    public static RotateServiceClientSecretResponse ToResponse(
        this RotateServiceClientSecretResult result) =>
        new(result.ClientId, result.ClientSecret);
}
