using FiapCloudGames.Identity.Application.Features.Clients;

namespace FiapCloudGames.Identity.Presentation.Features.Clients;

public sealed record ServiceClientResponse(
    string ClientId,
    string DisplayName,
    bool CanIssueUserTokens,
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ServiceClientResponse From(ServiceClientResult result) =>
        new(
            result.ClientId,
            result.DisplayName,
            result.CanIssueUserTokens,
            result.AllowedAudiences,
            result.AllowedScopes,
            result.IsActive,
            result.CreatedAtUtc);
}
