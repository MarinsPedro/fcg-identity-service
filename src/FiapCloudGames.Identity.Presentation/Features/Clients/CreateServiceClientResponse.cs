namespace FiapCloudGames.Identity.Presentation.Features.Clients;

public sealed record CreateServiceClientResponse(
    ServiceClientResponse Client,
    string ClientSecret);
