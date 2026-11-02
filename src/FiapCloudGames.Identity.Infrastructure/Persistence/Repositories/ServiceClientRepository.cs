using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;

internal sealed class ServiceClientRepository(IdentityDbContext context)
    : IServiceClientRepository
{
    public Task<ServiceClient?> FindByIdAsync(
        string clientId,
        CancellationToken cancellationToken) =>
        context.ServiceClients
            .Include(client => client.AllowedAudiences)
            .Include(client => client.AllowedScopes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
            client => client.ClientId == clientId,
            cancellationToken);

    public Task AddAsync(ServiceClient client, CancellationToken cancellationToken) =>
        context.ServiceClients.AddAsync(client, cancellationToken).AsTask();
}
