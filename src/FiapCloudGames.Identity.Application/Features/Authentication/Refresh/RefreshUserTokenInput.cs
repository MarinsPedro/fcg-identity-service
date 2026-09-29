namespace FiapCloudGames.Identity.Application.Features.Authentication.Refresh;

public sealed record RefreshUserTokenInput(
    string RefreshToken,
    string ClientId,
    string ClientSecret);
