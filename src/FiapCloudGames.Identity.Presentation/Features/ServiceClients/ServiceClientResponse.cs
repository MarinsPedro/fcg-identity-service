using FiapCloudGames.Identity.Application.Features.ServiceClients;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Representa os dados públicos e as permissões de um service client.
/// </summary>
/// <param name="ClientId">Identificador único do client.</param>
/// <param name="DisplayName">Nome de exibição do client.</param>
/// <param name="CanIssueUserTokens">Indica se o client pode iniciar sessões de usuários.</param>
/// <param name="AllowedAudiences">Audiences permitidas para o client.</param>
/// <param name="AllowedScopes">Scopes permitidos para o client.</param>
/// <param name="IsActive">Indica se o client está ativo.</param>
/// <param name="CreatedAtUtc">Data e hora de criação em UTC.</param>
public sealed record ServiceClientResponse(
    string ClientId,
    string DisplayName,
    bool CanIssueUserTokens,
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>
    /// Converte o resultado da aplicação em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado do service client.</param>
    /// <returns>Resposta com os dados públicos e as permissões do client.</returns>
    public static ServiceClientResponse From(ServiceClientResult result) =>
        new(
            result.ClientId,
            result.DisplayName,
            result.CanIssueUserTokens,
            result.AllowedAudiences,
            result.AllowedScopes,
            result.IsActive,
            result.CreatedAtUtc);
}
