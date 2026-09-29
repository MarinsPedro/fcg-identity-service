using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Application.Services;

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
