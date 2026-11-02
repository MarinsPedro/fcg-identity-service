using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Users;

/// <summary>
/// Expõe à borda os limites das entradas de usuário definidos pelo domínio.
/// </summary>
public static class UserInputConstraints
{
    public const int NameMinimumLength = User.NameMinimumLength;
    public const int NameMaximumLength = User.NameMaximumLength;
    public const int EmailMaximumLength = Email.MaximumLength;
    public const int PasswordMinimumLength = Password.MinimumLength;
}
