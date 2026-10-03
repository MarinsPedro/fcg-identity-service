namespace FiapCloudGames.Identity.Application.Features.Clients.RotateSecret;

public sealed record RotateServiceClientSecretResult(
    string ClientId,
    string ClientSecret);
