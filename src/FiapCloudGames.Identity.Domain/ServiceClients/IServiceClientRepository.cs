namespace FiapCloudGames.Identity.Domain.ServiceClients;

public interface IServiceClientRepository
{
    Task<ServiceClient?> FindByIdAsync(string clientId, CancellationToken cancellationToken);
    Task AddAsync(ServiceClient client, CancellationToken cancellationToken);
}
