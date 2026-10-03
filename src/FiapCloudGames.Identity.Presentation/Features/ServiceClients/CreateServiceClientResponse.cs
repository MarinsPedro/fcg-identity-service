namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

public sealed record CreateServiceClientResponse(
    ServiceClientResponse Client,
    string ClientSecret);
