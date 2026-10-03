using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Application.Features.Clients;

public sealed record ServiceClientResult(
    string ClientId,
    string DisplayName,
    bool CanIssueUserTokens,
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ServiceClientResult From(ServiceClient client) =>
        new(
            client.ClientId,
            client.DisplayName,
            client.CanIssueUserTokens,
            client.AllowedAudiences
                .Select(item => item.Audience)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            client.AllowedScopes
                .Select(item => item.Scope)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            client.IsActive,
            client.CreatedAtUtc);
}
