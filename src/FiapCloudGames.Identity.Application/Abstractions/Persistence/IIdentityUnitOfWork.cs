namespace FiapCloudGames.Identity.Application.Abstractions.Persistence;

public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
