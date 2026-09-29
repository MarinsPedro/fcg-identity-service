using FiapCloudGames.Identity.Application.Features.Users;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Refresh;

public sealed record RefreshUserTokenResult(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    UserResult User);
