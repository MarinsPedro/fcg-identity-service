namespace FiapCloudGames.Identity.Application.Models;

public sealed record UserResult(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record TokenPairResult(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    UserResult User);

public sealed record ServiceTokenResult(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string Scope);
