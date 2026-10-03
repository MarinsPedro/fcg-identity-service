using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Refresh;

public sealed class RefreshUserTokenService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityUnitOfWork unitOfWork,
    IRefreshTokenProtector refreshTokens,
    ITokenIssuer tokenIssuer,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    private static readonly string[] UserScopes = ["store.access"];

    public async Task<RefreshUserTokenResult> ExecuteAsync(
        RefreshUserTokenInput input,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            input.ClientId,
            input.ClientSecret,
            cancellationToken);
        if (!client.CanIssueUserTokens ||
            !client.AllowsAudience(client.ClientId) ||
            !client.AllowsScopes(UserScopes))
        {
            throw AppException.Forbidden("O client não pode renovar tokens de usuário.");
        }

        var now = clock.GetUtcNow();
        var current = await refreshTokenRepository.FindByHashAsync(
            refreshTokens.Hash(input.RefreshToken),
            cancellationToken);

        if (current is null ||
            !current.IsUsable(now) ||
            !string.Equals(current.ClientId, client.ClientId, StringComparison.Ordinal))
        {
            throw AppException.Authentication("Refresh token inválido.");
        }

        var user = await users.FindByIdAsync(current.UserId, cancellationToken);
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
        await refreshTokenRepository.AddAsync(nextToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshUserTokenResult(
            accessToken.AccessToken,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAtUtc - now).TotalSeconds),
            nextRawToken,
            IdentityApplicationMappings.ToResult(user));
    }
}
