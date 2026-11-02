using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;

namespace FiapCloudGames.Identity.Presentation.Features.OAuth;

/// <summary>
/// Fornece os mapeamentos HTTP para o fluxo OAuth client credentials.
/// </summary>
internal static class OAuthHttpMappings
{
    /// <summary>
    /// Converte o resultado da emissão em uma resposta OAuth.
    /// </summary>
    /// <param name="result">Resultado retornado pelo serviço de emissão.</param>
    /// <returns>Resposta OAuth com o token de serviço.</returns>
    public static ServiceTokenResponse ToResponse(this ServiceTokenResult result) =>
        new(result.AccessToken, result.TokenType, result.ExpiresIn, result.Scope);
}
