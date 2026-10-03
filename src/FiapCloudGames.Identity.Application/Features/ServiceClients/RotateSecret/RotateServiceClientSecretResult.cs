namespace FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;

public sealed record RotateServiceClientSecretResult(
    string ClientId,
    string ClientSecret);
