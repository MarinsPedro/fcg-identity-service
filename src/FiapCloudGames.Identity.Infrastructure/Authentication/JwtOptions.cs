namespace FiapCloudGames.Identity.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "fiap-cloud-games-identity";
    public string? PrivateKeyPem { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string KeyId { get; set; } = "identity-key-1";
    public TimeSpan UserTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ServiceTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    public bool AllowEphemeralDevelopmentKey { get; set; }
}
