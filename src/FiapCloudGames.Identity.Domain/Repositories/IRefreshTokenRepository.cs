using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
