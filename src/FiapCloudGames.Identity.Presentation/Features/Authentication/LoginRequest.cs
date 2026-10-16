using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Representa as credenciais de um usuário para iniciar uma sessão.
/// </summary>
/// <param name="Email">Endereço de e-mail do usuário.</param>
/// <param name="Password">Senha do usuário.</param>
public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required] string Password);
