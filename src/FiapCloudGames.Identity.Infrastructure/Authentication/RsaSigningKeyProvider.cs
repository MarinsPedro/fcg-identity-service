using System.Security.Cryptography;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FiapCloudGames.Identity.Infrastructure.Authentication;

internal sealed class RsaSigningKeyProvider : ITokenDiscoveryProvider, IDisposable
{
    private readonly RSA rsa;
    private readonly JwtOptions options;

    public RsaSigningKeyProvider(
        IOptions<JwtOptions> optionsAccessor,
        IHostEnvironment environment)
    {
        options = optionsAccessor.Value;
        rsa = RSA.Create();

        var pem = options.PrivateKeyPem;
        if (string.IsNullOrWhiteSpace(pem) && !string.IsNullOrWhiteSpace(options.PrivateKeyPath))
        {
            pem = File.ReadAllText(options.PrivateKeyPath);
        }

        if (!string.IsNullOrWhiteSpace(pem))
        {
            rsa.ImportFromPem(pem);
        }
        else if (environment.IsDevelopment() && options.AllowEphemeralDevelopmentKey)
        {
            rsa.KeySize = 2048;
        }
        else
        {
            throw new InvalidOperationException(
                "Configure Jwt:PrivateKeyPem ou Jwt:PrivateKeyPath. Chave efêmera é permitida somente em Development.");
        }

        SecurityKey = new RsaSecurityKey(rsa) { KeyId = options.KeyId };
    }

    public RsaSecurityKey SecurityKey { get; }

    public string Issuer => options.Issuer;

    public SigningCredentials SigningCredentials =>
        new(SecurityKey, SecurityAlgorithms.RsaSha256);

    public JwksDocument GetJwks()
    {
        var parameters = rsa.ExportParameters(false);
        return new JwksDocument(
        [
            new Jwk(
                "RSA",
                "sig",
                SecurityAlgorithms.RsaSha256,
                SecurityKey.KeyId,
                Base64UrlEncoder.Encode(parameters.Modulus),
                Base64UrlEncoder.Encode(parameters.Exponent))
        ]);
    }

    public void Dispose() => rsa.Dispose();
}
