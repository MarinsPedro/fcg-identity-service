using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class LoginService(
    IIdentityStore store,
    IPasswordHasher hasher,
    IRefreshTokenProtector refreshTokens,
    ITokenIssuer tokenIssuer,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    private static readonly string[] UserScopes = ["store.access"];

    public async Task<TokenPairResult> ExecuteAsync(
        string emailValue,
        string password,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            clientId,
            clientSecret,
            cancellationToken);

        if (!client.CanIssueUserTokens || !client.AllowsAudience(client.ClientId))
        {
            throw AppException.Forbidden("O client não pode autenticar usuários.");
        }

        if (!Email.TryCreate(emailValue, out var email))
        {
            throw InvalidCredentials();
        }

        var user = await store.FindUserByEmailAsync(email, cancellationToken);
        if (user is null || !hasher.Verify(password, user.PasswordHash))
        {
            throw InvalidCredentials();
        }

        if (!user.IsActive)
        {
            throw AppException.Forbidden("O usuário está inativo.");
        }

        var scopes = UserScopes.Where(scope => client.AllowsScopes([scope])).ToArray();
        if (scopes.Length == 0)
        {
            throw AppException.Forbidden("O client não possui scope de usuário.");
        }

        var accessToken = tokenIssuer.IssueUserToken(user, client, scopes);
        var rawRefreshToken = refreshTokens.Generate();
        var now = clock.GetUtcNow();
        var refreshToken = RefreshToken.Create(
            user.Id,
            client.ClientId,
            refreshTokens.Hash(rawRefreshToken),
            now,
            now.Add(tokenIssuer.RefreshTokenLifetime));

        await store.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return new TokenPairResult(
            accessToken.AccessToken,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAtUtc - now).TotalSeconds),
            rawRefreshToken,
            IdentityApplicationMappings.ToResult(user));
    }

    private static AppException InvalidCredentials() =>
        AppException.Authentication("E-mail ou senha inválidos.");
}
