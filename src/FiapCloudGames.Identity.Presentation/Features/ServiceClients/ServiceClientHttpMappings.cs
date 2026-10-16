using FiapCloudGames.Identity.Application.Features.ServiceClients.Create;
using FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Fornece os mapeamentos HTTP para gerenciamento de service clients.
/// </summary>
internal static class ServiceClientHttpMappings
{
    /// <summary>
    /// Converte a requisição de criação em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição de criação do service client.</param>
    /// <returns>Entrada esperada pelo serviço de criação.</returns>
    public static CreateServiceClientInput ToInput(
        this CreateServiceClientRequest request) =>
        new(
            request.ClientId,
            request.DisplayName,
            request.CanIssueUserTokens,
            request.AllowedAudiences,
            request.AllowedScopes);

    /// <summary>
    /// Converte a requisição de permissões em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição com o conjunto completo de permissões.</param>
    /// <returns>Entrada esperada pelo serviço de atualização.</returns>
    public static UpdateServiceClientPermissionsInput ToInput(
        this UpdateServiceClientPermissionsRequest request) =>
        new(request.AllowedAudiences, request.AllowedScopes);

    /// <summary>
    /// Converte o resultado da criação em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado retornado pelo serviço de criação.</param>
    /// <returns>Client criado e segredo inicial.</returns>
    public static CreateServiceClientResponse ToResponse(
        this CreateServiceClientResult result) =>
        new(ServiceClientResponse.From(result.Client), result.ClientSecret);

    /// <summary>
    /// Converte o resultado da rotação em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado retornado pelo serviço de rotação.</param>
    /// <returns>Novo segredo do service client.</returns>
    public static RotateServiceClientSecretResponse ToResponse(
        this RotateServiceClientSecretResult result) =>
        new(result.ClientId, result.ClientSecret);
}
