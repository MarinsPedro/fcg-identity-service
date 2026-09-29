namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

public sealed record ServiceTokenResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string Scope);
