using FiapCloudGames.Identity.Application.Features.Users.CreateUser;

namespace FiapCloudGames.Identity.Presentation.Features.Users.CreateUser;

internal static class CreateUserHttpMappings
{
    public static CreateUserInput ToInput(this CreateUserRequest request) =>
        new(request.Name, request.Email, request.Password);
}
