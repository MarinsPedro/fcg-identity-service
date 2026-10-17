using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Presentation.Validation;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Representa os dados necessários para criar um usuário.
/// </summary>
/// <param name="Name">Nome do usuário.</param>
/// <param name="Email">Endereço de e-mail do usuário.</param>
/// <param name="Password">Senha inicial do usuário.</param>
public sealed record CreateUserRequest(
    [Required]
    [ApiStringLength(
        UserInputConstraints.NameMaximumLength,
        MinimumLength = UserInputConstraints.NameMinimumLength)]
    string Name,
    [Required, EmailAddress]
    [ApiStringLength(UserInputConstraints.EmailMaximumLength)]
    string Email,
    [Required, MinLength(UserInputConstraints.PasswordMinimumLength)]
    string Password);
