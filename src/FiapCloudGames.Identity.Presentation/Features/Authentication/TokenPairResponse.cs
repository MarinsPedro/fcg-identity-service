using FiapCloudGames.Identity.Presentation.Features.Users;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

public sealed record TokenPairResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    UserResponse User);
