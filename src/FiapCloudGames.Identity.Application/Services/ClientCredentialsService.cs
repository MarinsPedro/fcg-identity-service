using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Models;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class ClientCredentialsService(
    ClientAuthenticator clientAuthenticator,
    ITokenIssuer tokenIssuer,
    TimeProvider clock)
{
    public async Task<ServiceTokenResult> ExecuteAsync(
        string grantType,
        string clientId,
        string clientSecret,
        string audience,
        string scope,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(grantType, "client_credentials", StringComparison.Ordinal))
        {
            throw AppException.Validation("grant_type não suportado.");
        }

        var client = await clientAuthenticator.AuthenticateAsync(
            clientId,
            clientSecret,
            cancellationToken);
        var scopes = NormalizeScopes(scope);

        if (!client.AllowsAudience(audience) || !client.AllowsScopes(scopes))
        {
            throw AppException.Forbidden("Audience ou scopes não permitidos para o client.");
        }

        var token = tokenIssuer.IssueServiceToken(client, audience, scopes);
        var expiresIn = Math.Max(0, (long)(token.ExpiresAtUtc - clock.GetUtcNow()).TotalSeconds);
        return new ServiceTokenResult(
            token.AccessToken,
            "Bearer",
            expiresIn,
            string.Join(' ', scopes));
    }

    private static string[] NormalizeScopes(string scope)
    {
        var scopes = scope.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return scopes.Length > 0
            ? scopes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
            : throw AppException.Validation("Informe ao menos um scope.");
    }
}
