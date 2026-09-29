namespace FiapCloudGames.Identity.Presentation.Features.Users;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
