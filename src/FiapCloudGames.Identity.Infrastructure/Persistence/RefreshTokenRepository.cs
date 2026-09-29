using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence;

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
