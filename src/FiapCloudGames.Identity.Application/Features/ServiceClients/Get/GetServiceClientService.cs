using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Get;

public sealed class GetServiceClientService(
    IServiceClientRepository clients,
    ILogger<GetServiceClientService> logger)
{
    public async Task<ServiceClientResult> ExecuteAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Consultando service client {ClientId}.",
            clientId);

        var client = await clients.FindByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogDebug(
                "Service client {ClientId} não encontrado.",
                clientId);
            throw AppException.NotFound("Client não encontrado.");
        }

        return ServiceClientResult.From(client);
    }
}
