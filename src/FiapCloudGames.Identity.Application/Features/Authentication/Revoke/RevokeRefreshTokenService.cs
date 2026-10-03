using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Domain.Sessions;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Revoke;

public sealed class RevokeRefreshTokenService(
    IRefreshTokenRepository refreshTokensRepository,
    IIdentityUnitOfWork unitOfWork,
    IRefreshTokenProtector refreshTokens,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    public async Task ExecuteAsync(
        RevokeRefreshTokenInput input,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            input.ClientId,
            input.ClientSecret,
            cancellationToken);
        var token = await refreshTokensRepository.FindByHashAsync(
            refreshTokens.Hash(input.RefreshToken),
            cancellationToken);

        if (token is null ||
            !string.Equals(token.ClientId, client.ClientId, StringComparison.Ordinal))
        {
            return;
        }

        token.Revoke(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
