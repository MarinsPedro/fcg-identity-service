using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Representa os dados permitidos na atualização de um usuário.
/// </summary>
/// <param name="Name">Novo nome do usuário.</param>
/// <param name="Email">Novo endereço de e-mail do usuário.</param>
public sealed record UpdateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email);
