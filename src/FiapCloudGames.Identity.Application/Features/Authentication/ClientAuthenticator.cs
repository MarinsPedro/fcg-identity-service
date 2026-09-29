using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Authentication;

public sealed class ClientAuthenticator(
    IServiceClientRepository clients,
    IPasswordHasher hasher)
{
    public async Task<ServiceClient> AuthenticateAsync(
        string? clientId,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw InvalidClient();
        }

        var client = await clients.FindByIdAsync(clientId.Trim(), cancellationToken);
        if (client is null || !client.IsActive || !hasher.Verify(clientSecret, client.SecretHash))
        {
            throw InvalidClient();
        }

        return client;
    }

    private static AppException InvalidClient() =>
        AppException.Authentication("Client inválido.");
}
