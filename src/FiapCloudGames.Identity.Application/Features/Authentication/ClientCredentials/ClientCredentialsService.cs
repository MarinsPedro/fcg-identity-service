using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;

public sealed class ClientCredentialsService(
    ClientAuthenticator clientAuthenticator,
    ITokenIssuer tokenIssuer,
    TimeProvider clock,
    ILogger<ClientCredentialsService> logger)
{
    public async Task<ServiceTokenResult> ExecuteAsync(
        ClientCredentialsInput input,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Iniciando autenticação de service client.");

        if (!string.Equals(input.GrantType, "client_credentials", StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Emissão de token de serviço rejeitada por grant_type não suportado.");
            throw AppException.Validation("grant_type não suportado.");
        }

        var client = await clientAuthenticator.AuthenticateAsync(
            input.ClientId,
            input.ClientSecret,
            cancellationToken);
        var scopes = NormalizeScopes(input.Scope);

        if (!client.AllowsAudience(input.Audience) || !client.AllowsScopes(scopes))
        {
            logger.LogWarning(
                "Emissão de token rejeitada para o client {ClientId}, audience {Audience} e {ScopeCount} scope(s).",
                client.ClientId,
                input.Audience,
                scopes.Length);
            throw AppException.Forbidden("Audience ou scopes não permitidos para o client.");
        }

        var token = tokenIssuer.IssueServiceToken(client, input.Audience, scopes);
        var expiresIn = Math.Max(0, (long)(token.ExpiresAtUtc - clock.GetUtcNow()).TotalSeconds);
        logger.LogInformation(
            "Token de serviço emitido para o client {ClientId}, audience {Audience} e {ScopeCount} scope(s).",
            client.ClientId,
            input.Audience,
            scopes.Length);
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
