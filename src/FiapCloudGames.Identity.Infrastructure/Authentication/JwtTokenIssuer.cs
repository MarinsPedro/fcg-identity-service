using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FiapCloudGames.Identity.Infrastructure.Authentication;

internal sealed class JwtTokenIssuer(
    RsaSigningKeyProvider keys,
    IOptions<JwtOptions> optionsAccessor,
    TimeProvider clock) : ITokenIssuer
{
    private readonly JwtOptions options = optionsAccessor.Value;

    public TimeSpan RefreshTokenLifetime => options.RefreshTokenLifetime;

    public AccessTokenResult IssueUserToken(
        User user,
        ServiceClient client,
        IReadOnlyCollection<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(FiapClaimTypes.ClientId, client.ClientId),
            new(FiapClaimTypes.TokenUse, FiapTokenUses.User),
            new(FiapClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value)
        };
        AddScopes(claims, scopes);
        return Issue(client.ClientId, claims, options.UserTokenLifetime);
    }

    public AccessTokenResult IssueServiceToken(
        ServiceClient client,
        string audience,
        IReadOnlyCollection<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.ClientId),
            new(FiapClaimTypes.ClientId, client.ClientId),
            new(FiapClaimTypes.TokenUse, FiapTokenUses.Service)
        };
        AddScopes(claims, scopes);
        return Issue(audience, claims, options.ServiceTokenLifetime);
    }

    private AccessTokenResult Issue(
        string audience,
        IEnumerable<Claim> claims,
        TimeSpan lifetime)
    {
        var issuedAt = clock.GetUtcNow();
        var expiresAt = issuedAt.Add(lifetime);
        var allClaims = claims.Concat(
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        ]);

        var token = new JwtSecurityToken(
            options.Issuer,
            audience,
            allClaims,
            issuedAt.UtcDateTime,
            expiresAt.UtcDateTime,
            keys.SigningCredentials);
        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private static void AddScopes(ICollection<Claim> claims, IEnumerable<string> scopes)
    {
        foreach (var scope in scopes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            claims.Add(new Claim(FiapClaimTypes.Scope, scope));
        }
    }
}
