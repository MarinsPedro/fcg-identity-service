using FiapCloudGames.Identity.Application.Features.Users.Create;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Fornece os mapeamentos HTTP para criação de usuários.
/// </summary>
internal static class CreateUserHttpMappings
{
    /// <summary>
    /// Converte a requisição HTTP em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição de criação do usuário.</param>
    /// <returns>Entrada esperada pelo serviço de criação.</returns>
    public static CreateUserInput ToInput(this CreateUserRequest request) =>
        new(request.Name, request.Email, request.Password);
}
