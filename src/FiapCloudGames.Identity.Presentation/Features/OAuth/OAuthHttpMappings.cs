using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;

namespace FiapCloudGames.Identity.Presentation.Features.OAuth;

internal static class OAuthHttpMappings
{
    public static ServiceTokenResponse ToResponse(this ServiceTokenResult result) =>
        new(result.AccessToken, result.TokenType, result.ExpiresIn, result.Scope);
}
