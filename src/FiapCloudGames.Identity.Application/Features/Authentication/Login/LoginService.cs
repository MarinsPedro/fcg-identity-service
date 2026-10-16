using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Users;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Login;

public sealed class LoginService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    IRefreshTokenProtector refreshTokens,
    ITokenIssuer tokenIssuer,
    AuthenticatedClientResolver authenticatedClientResolver,
    TimeProvider clock,
    ILogger<LoginService> logger)
{
    private static readonly string[] UserScopes = ["store.access"];

    public async Task<LoginResult> ExecuteAsync(
        LoginInput input,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Iniciando autenticação de usuário para o client {ClientId}.",
            input.ClientId);

        var client = await authenticatedClientResolver.ResolveAsync(
            input.ClientId,
            cancellationToken);

        if (!client.CanIssueUserTokens || !client.AllowsAudience(client.ClientId))
        {
            logger.LogWarning(
                "Autenticação de usuário rejeitada: client {ClientId} não pode emitir tokens de usuário.",
                client.ClientId);
            throw AppException.Forbidden("O client não pode autenticar usuários.");
        }

        if (!Email.TryCreate(input.Email, out var email))
        {
            LogInvalidCredentials();
            throw InvalidCredentials();
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null || !hasher.Verify(input.Password, user.PasswordHash))
        {
            LogInvalidCredentials();
            throw InvalidCredentials();
        }

        if (!user.IsActive)
        {
            logger.LogInformation(
                "Autenticação rejeitada: usuário {UserId} está inativo.",
                user.Id);
            throw AppException.Forbidden("O usuário está inativo.");
        }

        var scopes = UserScopes.Where(scope => client.AllowsScopes([scope])).ToArray();
        if (scopes.Length == 0)
        {
            logger.LogWarning(
                "Autenticação de usuário rejeitada: client {ClientId} não possui scope de usuário.",
                client.ClientId);
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

        logger.LogInformation(
            "Usuário {UserId} autenticado com sucesso pelo client {ClientId}.",
            user.Id,
            client.ClientId);

        return new LoginResult(
            accessToken.AccessToken,
            "Bearer",
            Math.Max(0, (long)(accessToken.ExpiresAtUtc - now).TotalSeconds),
            rawRefreshToken,
            IdentityApplicationMappings.ToResult(user));
    }

    private static AppException InvalidCredentials() =>
        AppException.Authentication("E-mail ou senha inválidos.");

    private void LogInvalidCredentials() =>
        logger.LogInformation(
            "Autenticação de usuário rejeitada por credenciais inválidas.");
}
