using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(Password password);
    string HashSecret(string secret);
    bool Verify(string value, string hash);
}
