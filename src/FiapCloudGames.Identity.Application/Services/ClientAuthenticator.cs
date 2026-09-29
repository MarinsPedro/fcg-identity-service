using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class ClientAuthenticator(
    IIdentityStore store,
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

        var client = await store.FindClientAsync(clientId.Trim(), cancellationToken);
        if (client is null || !client.IsActive || !hasher.Verify(clientSecret, client.SecretHash))
        {
            throw InvalidClient();
        }

        return client;
    }

    private static AppException InvalidClient() =>
        AppException.Authentication("Client inválido.");
}
