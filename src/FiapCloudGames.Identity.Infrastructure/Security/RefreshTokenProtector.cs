using System.Security.Cryptography;
using System.Text;
using FiapCloudGames.Identity.Application.Abstractions.Security;

namespace FiapCloudGames.Identity.Infrastructure.Security;

internal sealed class RefreshTokenProtector : IRefreshTokenProtector
{
    public string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
