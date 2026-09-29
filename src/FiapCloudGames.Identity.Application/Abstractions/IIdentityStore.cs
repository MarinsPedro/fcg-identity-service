using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Abstractions;

public interface IIdentityStore
{
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task<User?> FindUserByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> UserEmailExistsAsync(Email email, Guid? exceptUserId, CancellationToken cancellationToken);
    Task<ServiceClient?> FindClientAsync(string clientId, CancellationToken cancellationToken);
    Task AddClientAsync(ServiceClient client, CancellationToken cancellationToken);
    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
