using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FiapCloudGames.Identity.Infrastructure.Authentication;

public interface IJwksProvider
{
    object GetDocument();
}

internal sealed class RsaSigningKeyProvider : IJwksProvider, IDisposable
{
    private readonly RSA rsa;

    public RsaSigningKeyProvider(
        IOptions<JwtOptions> optionsAccessor,
        IHostEnvironment environment)
    {
        var options = optionsAccessor.Value;
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

    public SigningCredentials SigningCredentials =>
        new(SecurityKey, SecurityAlgorithms.RsaSha256);

    public object GetDocument()
    {
        var parameters = rsa.ExportParameters(false);
        return new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = SecurityAlgorithms.RsaSha256,
                    kid = SecurityKey.KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };
    }

    public void Dispose() => rsa.Dispose();
}
