using FiapCloudGames.Identity.Application.Features.Users.Update;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Fornece os mapeamentos HTTP para atualização de usuários.
/// </summary>
internal static class UpdateUserHttpMappings
{
    /// <summary>
    /// Converte a requisição HTTP em entrada da aplicação.
    /// </summary>
    /// <param name="request">Requisição de atualização do usuário.</param>
    /// <returns>Entrada esperada pelo serviço de atualização.</returns>
    public static UpdateUserInput ToInput(this UpdateUserRequest request) =>
        new(request.Name, request.Email);
}
