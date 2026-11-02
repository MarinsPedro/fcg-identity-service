namespace FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;

public sealed record ServiceTokenResult(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string Scope);
