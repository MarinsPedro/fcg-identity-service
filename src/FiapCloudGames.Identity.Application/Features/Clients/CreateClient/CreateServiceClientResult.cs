namespace FiapCloudGames.Identity.Application.Features.Clients.CreateClient;

public sealed record CreateServiceClientResult(
    ServiceClientResult Client,
    string ClientSecret);
