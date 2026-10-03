namespace FiapCloudGames.Identity.Application.Features.Clients.CreateClient;

public sealed record CreateServiceClientInput(
    string ClientId,
    string DisplayName,
    bool CanIssueUserTokens,
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes);
