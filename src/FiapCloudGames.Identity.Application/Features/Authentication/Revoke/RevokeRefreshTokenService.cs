using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Domain.Sessions;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Revoke;

public sealed class RevokeRefreshTokenService(
    IRefreshTokenRepository refreshTokensRepository,
    IIdentityUnitOfWork unitOfWork,
    IRefreshTokenProtector refreshTokens,
    AuthenticatedClientResolver authenticatedClientResolver,
    TimeProvider clock,
    ILogger<RevokeRefreshTokenService> logger)
{
    public async Task ExecuteAsync(
        RevokeRefreshTokenInput input,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Iniciando revogação de refresh token para o client {ClientId}.",
            input.ClientId);

        var client = await authenticatedClientResolver.ResolveAsync(
            input.ClientId,
            cancellationToken);
        var token = await refreshTokensRepository.FindByHashAsync(
            refreshTokens.Hash(input.RefreshToken),
            cancellationToken);

        if (token is null ||
            !string.Equals(token.ClientId, client.ClientId, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Revogação solicitada para refresh token inexistente ou pertencente a outro client.");
            return;
        }

        token.Revoke(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Refresh token do usuário {UserId} revogado pelo client {ClientId}.",
            token.UserId,
            client.ClientId);
    }
}
