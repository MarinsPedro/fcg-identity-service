namespace FiapCloudGames.Identity.Presentation.Features.Clients;

public sealed record RotateServiceClientSecretResponse(
    string ClientId,
    string ClientSecret);
