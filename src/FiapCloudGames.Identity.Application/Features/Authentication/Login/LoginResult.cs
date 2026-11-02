using FiapCloudGames.Identity.Application.Features.Users;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Login;

public sealed record LoginResult(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    UserResult User);
