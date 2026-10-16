using FiapCloudGames.Identity.Presentation.Features.Users;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Representa os tokens e os dados retornados para uma sessão de usuário.
/// </summary>
/// <param name="AccessToken">JWT de acesso emitido para o usuário.</param>
/// <param name="TokenType">Tipo do token de acesso.</param>
/// <param name="ExpiresIn">Tempo de validade do access token, em segundos.</param>
/// <param name="RefreshToken">Token opaco usado para renovar a sessão.</param>
/// <param name="User">Dados do usuário autenticado.</param>
public sealed record TokenPairResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    UserResponse User);
