using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class RefreshUserTokenService(
    IIdentityStore store,
    IRefreshTokenProtector refreshTokens,
    ITokenIssuer tokenIssuer,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    private static readonly string[] UserScopes = ["store.access"];

    public async Task<TokenPairResult> ExecuteAsync(
        string rawRefreshToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            clientId,
            clientSecret,
            cancellationToken);
        if (!client.CanIssueUserTokens ||
            !client.AllowsAudience(client.ClientId) ||
            !client.AllowsScopes(UserScopes))
        {
            throw AppException.Forbidden("O client não pode renovar tokens de usuário.");
        }

        var now = clock.GetUtcNow();
        var current = await store.FindRefreshTokenAsync(
            refreshTokens.Hash(rawRefreshToken),
            cancellationToken);

        if (current is null ||
            !current.IsUsable(now) ||
            !string.Equals(current.ClientId, client.ClientId, StringComparison.Ordinal))
        {
            throw AppException.Authentication("Refresh token inválido.");
        }

        var user = await store.FindUserByIdAsync(current.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw AppException.Authentication("Refresh token inválido.");
        }

        var accessToken = tokenIssuer.IssueUserToken(user, client, UserScopes);
        var nextRawToken = refreshTokens.Generate();
        var nextToken = RefreshToken.Create(
            user.Id,
            client.ClientId,
            refreshTokens.Hash(nextRawToken),
            now,
            now.Add(tokenIssuer.RefreshTokenLifetime));

        current.Rotate(nextToken.Id, now);
        await store.AddRefreshTokenAsync(nextToken, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return new TokenPairResult(
            accessToken.AccessToken,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAtUtc - now).TotalSeconds),
            nextRawToken,
            IdentityApplicationMappings.ToResult(user));
    }
}
