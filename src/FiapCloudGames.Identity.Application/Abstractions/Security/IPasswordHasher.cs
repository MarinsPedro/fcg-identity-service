using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(Password password);
    string HashSecret(string secret);
    bool Verify(string value, string hash);
}
