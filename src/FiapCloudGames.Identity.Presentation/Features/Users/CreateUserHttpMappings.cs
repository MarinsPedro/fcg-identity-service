using FiapCloudGames.Identity.Application.Features.Users.Create;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

internal static class CreateUserHttpMappings
{
    public static CreateUserInput ToInput(this CreateUserRequest request) =>
        new(request.Name, request.Email, request.Password);
}
