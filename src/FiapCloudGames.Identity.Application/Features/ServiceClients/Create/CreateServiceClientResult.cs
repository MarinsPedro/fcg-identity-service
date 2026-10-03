namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Create;

public sealed record CreateServiceClientResult(
    ServiceClientResult Client,
    string ClientSecret);
