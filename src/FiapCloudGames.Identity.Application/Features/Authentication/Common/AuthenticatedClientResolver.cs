using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Common;

public sealed class AuthenticatedClientResolver(
    IServiceClientRepository clients,
    ILogger<AuthenticatedClientResolver> logger)
{
    public async Task<ServiceClient> ResolveAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindByIdAsync(clientId, cancellationToken);
        if (client is null || !client.IsActive)
        {
            logger.LogWarning(
                "Service client autenticado {ClientId} não está ativo.",
                clientId);
            throw AppException.Forbidden("O client autenticado não está ativo.");
        }

        return client;
    }
}
