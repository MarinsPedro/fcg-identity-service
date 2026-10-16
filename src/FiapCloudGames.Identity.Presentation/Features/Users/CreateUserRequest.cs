using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Representa os dados necessários para criar um usuário.
/// </summary>
/// <param name="Name">Nome do usuário.</param>
/// <param name="Email">Endereço de e-mail do usuário.</param>
/// <param name="Password">Senha inicial do usuário.</param>
public sealed record CreateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, MinLength(8)] string Password);
