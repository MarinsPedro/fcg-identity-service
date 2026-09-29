using FiapCloudGames.Identity.Application.Abstractions;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class RevokeRefreshTokenService(
    IIdentityStore store,
    IRefreshTokenProtector refreshTokens,
    ClientAuthenticator clientAuthenticator,
    TimeProvider clock)
{
    public async Task ExecuteAsync(
        string rawRefreshToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var client = await clientAuthenticator.AuthenticateAsync(
            clientId,
            clientSecret,
            cancellationToken);
        var token = await store.FindRefreshTokenAsync(
            refreshTokens.Hash(rawRefreshToken),
            cancellationToken);

        if (token is null ||
            !string.Equals(token.ClientId, client.ClientId, StringComparison.Ordinal))
        {
            return;
        }

        token.Revoke(clock.GetUtcNow());
        await store.SaveChangesAsync(cancellationToken);
    }
}
