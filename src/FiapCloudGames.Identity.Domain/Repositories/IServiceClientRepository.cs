using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Domain.Repositories;

public interface IServiceClientRepository
{
    Task<ServiceClient?> FindByIdAsync(string clientId, CancellationToken cancellationToken);
    Task AddAsync(ServiceClient client, CancellationToken cancellationToken);
}
