using System.Security.Cryptography;
using FiapCloudGames.Identity.Application.Abstractions;

namespace FiapCloudGames.Identity.Infrastructure.Security;

internal sealed class ClientSecretGenerator : IClientSecretGenerator
{
    private const int SecretSize = 32;

    public string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretSize))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
