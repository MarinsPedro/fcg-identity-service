using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Clients.GetClient;

public sealed class GetServiceClientService(IServiceClientRepository clients)
{
    public async Task<ServiceClientResult> ExecuteAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw AppException.NotFound("Client não encontrado.");
        return ServiceClientResult.From(client);
    }
}
