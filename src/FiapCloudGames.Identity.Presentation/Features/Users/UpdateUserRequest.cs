using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Presentation.Validation;

namespace FiapCloudGames.Identity.Presentation.Features.Users;

/// <summary>
/// Representa os dados permitidos na atualização de um usuário.
/// </summary>
/// <param name="Name">Novo nome do usuário.</param>
/// <param name="Email">Novo endereço de e-mail do usuário.</param>
public sealed record UpdateUserRequest(
    [Required]
    [ApiStringLength(
        UserInputConstraints.NameMaximumLength,
        MinimumLength = UserInputConstraints.NameMinimumLength)]
    string Name,
    [Required, EmailAddress]
    [ApiStringLength(UserInputConstraints.EmailMaximumLength)]
    string Email);
