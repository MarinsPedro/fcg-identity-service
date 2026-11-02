namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Representa os dados de um usuário retornados pela API.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Name">Nome do usuário.</param>
/// <param name="Email">Endereço de e-mail do usuário.</param>
/// <param name="Role">Papel atribuído ao usuário.</param>
/// <param name="IsActive">Indica se o usuário está ativo.</param>
/// <param name="CreatedAtUtc">Data e hora de criação em UTC.</param>
public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
