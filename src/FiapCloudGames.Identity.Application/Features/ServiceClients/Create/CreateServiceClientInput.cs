namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Create;

public sealed record CreateServiceClientInput(
    string ClientId,
    string DisplayName,
    bool CanIssueUserTokens,
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes);
