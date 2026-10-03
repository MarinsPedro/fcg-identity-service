using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Users;

internal static class IdentityApplicationMappings
{
    public static UserResult ToResult(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email.Value,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAtUtc);
}
