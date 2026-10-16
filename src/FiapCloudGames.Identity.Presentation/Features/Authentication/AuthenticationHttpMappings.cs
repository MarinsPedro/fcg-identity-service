using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.Presentation.Features.Users;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Fornece os mapeamentos HTTP para autenticação e sessões de usuários.
/// </summary>
internal static class AuthenticationHttpMappings
{
    /// <summary>
    /// Converte a requisição de login em entrada da aplicação.
    /// </summary>
    /// <param name="request">Credenciais do usuário.</param>
    /// <param name="clientId">Identificador do service client autenticado.</param>
    /// <returns>Entrada esperada pelo serviço de login.</returns>
    public static LoginInput ToInput(this LoginRequest request, string clientId) =>
        new(request.Email, request.Password, clientId);

    /// <summary>
    /// Converte a requisição de renovação em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição contendo o refresh token.</param>
    /// <param name="clientId">Identificador do service client autenticado.</param>
    /// <returns>Entrada esperada pelo serviço de renovação.</returns>
    public static RefreshUserTokenInput ToInput(this RefreshRequest request, string clientId) =>
        new(request.RefreshToken, clientId);

    /// <summary>
    /// Converte a requisição de revogação em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição contendo o refresh token.</param>
    /// <param name="clientId">Identificador do service client autenticado.</param>
    /// <returns>Entrada esperada pelo serviço de revogação.</returns>
    public static RevokeRefreshTokenInput ToInput(
        this RevokeRefreshTokenRequest request,
        string clientId) =>
        new(request.RefreshToken, clientId);

    /// <summary>
    /// Converte o resultado do login em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado retornado pelo serviço de login.</param>
    /// <returns>Par de tokens e dados do usuário.</returns>
    public static TokenPairResponse ToResponse(this LoginResult result) =>
        new(
            result.AccessToken,
            result.TokenType,
            result.ExpiresIn,
            result.RefreshToken,
            result.User.ToResponse());

    /// <summary>
    /// Converte o resultado da renovação em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado retornado pelo serviço de renovação.</param>
    /// <returns>Novo par de tokens e dados do usuário.</returns>
    public static TokenPairResponse ToResponse(this RefreshUserTokenResult result) =>
        new(
            result.AccessToken,
            result.TokenType,
            result.ExpiresIn,
            result.RefreshToken,
            result.User.ToResponse());
}
