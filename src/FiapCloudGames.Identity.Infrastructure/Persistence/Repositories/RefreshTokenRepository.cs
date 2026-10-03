using FiapCloudGames.Identity.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(IdentityDbContext context)
    : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        context.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        context.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
}
