using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Authentication.Common;

public sealed class ClientAuthenticator(
    IServiceClientRepository clients,
    IPasswordHasher hasher,
    ILogger<ClientAuthenticator> logger)
{
    public async Task<ServiceClient> AuthenticateAsync(
        string? clientId,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogInformation(
                "Autenticação de service client rejeitada por credenciais inválidas.");
            throw InvalidClient();
        }

        var client = await clients.FindByIdAsync(clientId.Trim(), cancellationToken);
        if (client is null || !client.IsActive || !hasher.Verify(clientSecret, client.SecretHash))
        {
            logger.LogInformation(
                "Autenticação de service client rejeitada por credenciais inválidas.");
            throw InvalidClient();
        }

        return client;
    }

    private static AppException InvalidClient() =>
        AppException.Authentication("Client inválido.");
}
