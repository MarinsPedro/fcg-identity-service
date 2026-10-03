using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Login;

public sealed class LoginService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    IRefreshTokenProtector refreshTokens,
    ITokenIssuer tokenIssuer,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    private static readonly string[] UserScopes = ["store.access"];

    public async Task<LoginResult> ExecuteAsync(
        LoginInput input,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            input.ClientId,
            input.ClientSecret,
            cancellationToken);

        if (!client.CanIssueUserTokens || !client.AllowsAudience(client.ClientId))
        {
            throw AppException.Forbidden("O client não pode autenticar usuários.");
        }

        if (!Email.TryCreate(input.Email, out var email))
        {
            throw InvalidCredentials();
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null || !hasher.Verify(input.Password, user.PasswordHash))
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

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            accessToken.AccessToken,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAtUtc - now).TotalSeconds),
            rawRefreshToken,
            IdentityApplicationMappings.ToResult(user));
    }

    private static AppException InvalidCredentials() =>
        AppException.Authentication("E-mail ou senha inválidos.");
}
