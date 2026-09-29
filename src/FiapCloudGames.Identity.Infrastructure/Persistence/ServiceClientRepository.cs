using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence;

internal sealed class ServiceClientRepository(IdentityDbContext context)
    : IServiceClientRepository
{
    public Task<ServiceClient?> FindByIdAsync(
        string clientId,
        CancellationToken cancellationToken) =>
        context.ServiceClients.SingleOrDefaultAsync(
            client => client.ClientId == clientId,
            cancellationToken);

    public Task AddAsync(ServiceClient client, CancellationToken cancellationToken) =>
        context.ServiceClients.AddAsync(client, cancellationToken).AsTask();
}
