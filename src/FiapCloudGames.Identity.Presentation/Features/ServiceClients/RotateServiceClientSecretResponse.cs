namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

public sealed record RotateServiceClientSecretResponse(
    string ClientId,
    string ClientSecret);
