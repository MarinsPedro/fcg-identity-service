using System.Security.Cryptography;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Infrastructure.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(Password password) => HashCore(password.Value);

    public string HashSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return HashCore(secret);
    }

    public bool Verify(string value, string hash)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var parts = hash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                value,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashCore(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            value,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
