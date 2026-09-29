using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.Presentation.Features.Users;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

internal static class AuthenticationHttpMappings
{
    public static LoginInput ToInput(this LoginRequest request) =>
        new(request.Email, request.Password, request.ClientId, request.ClientSecret);

    public static RefreshUserTokenInput ToInput(this RefreshRequest request) =>
        new(request.RefreshToken, request.ClientId, request.ClientSecret);

    public static RevokeRefreshTokenInput ToInput(this RevokeRefreshTokenRequest request) =>
        new(request.RefreshToken, request.ClientId, request.ClientSecret);

    public static TokenPairResponse ToResponse(this LoginResult result) =>
        new(
            result.AccessToken,
            result.TokenType,
            result.ExpiresIn,
            result.RefreshToken,
            result.User.ToResponse());

    public static TokenPairResponse ToResponse(this RefreshUserTokenResult result) =>
        new(
            result.AccessToken,
            result.TokenType,
            result.ExpiresIn,
            result.RefreshToken,
            result.User.ToResponse());

    public static ServiceTokenResponse ToResponse(this ServiceTokenResult result) =>
        new(result.AccessToken, result.TokenType, result.ExpiresIn, result.Scope);
}
