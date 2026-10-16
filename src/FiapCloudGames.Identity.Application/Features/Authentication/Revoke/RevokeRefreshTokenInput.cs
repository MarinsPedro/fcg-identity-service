namespace FiapCloudGames.Identity.Application.Features.Authentication.Revoke;

public sealed record RevokeRefreshTokenInput(
    string RefreshToken,
    string ClientId);
