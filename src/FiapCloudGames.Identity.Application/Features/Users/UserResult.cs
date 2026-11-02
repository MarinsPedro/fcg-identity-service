namespace FiapCloudGames.Identity.Application.Features.Users;

public sealed record UserResult(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
