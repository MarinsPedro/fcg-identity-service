using FiapCloudGames.Identity.Application.Features.Users.Update;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

internal static class UpdateUserHttpMappings
{
    public static UpdateUserInput ToInput(this UpdateUserRequest request) =>
        new(request.Name, request.Email);
}
