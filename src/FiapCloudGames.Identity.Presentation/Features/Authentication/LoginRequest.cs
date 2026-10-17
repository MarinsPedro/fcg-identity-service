using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Presentation.Validation;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Representa as credenciais de um usuário para iniciar uma sessão.
/// </summary>
/// <param name="Email">Endereço de e-mail do usuário.</param>
/// <param name="Password">Senha do usuário.</param>
public sealed record LoginRequest(
    [Required, EmailAddress]
    [ApiStringLength(UserInputConstraints.EmailMaximumLength)]
    string Email,
    [Required] string Password);
