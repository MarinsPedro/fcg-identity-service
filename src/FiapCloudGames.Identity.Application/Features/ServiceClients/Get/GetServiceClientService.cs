using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.ServiceClients;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Get;

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
