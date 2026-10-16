using FiapCloudGames.Identity.Application.Features.Users;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Fornece os mapeamentos HTTP para respostas de usuários.
/// </summary>
internal static class UserHttpMappings
{
    /// <summary>
    /// Converte o resultado da aplicação em resposta HTTP.
    /// </summary>
    /// <param name="result">Resultado de usuário retornado pela aplicação.</param>
    /// <returns>Resposta com os dados do usuário.</returns>
    public static UserResponse ToResponse(this UserResult result) =>
        new(
            result.Id,
            result.Name,
            result.Email,
            result.Role,
            result.IsActive,
            result.CreatedAtUtc);
}
