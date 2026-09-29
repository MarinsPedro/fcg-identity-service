using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.Application.Abstractions;

public interface ITokenIssuer
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessTokenResult IssueUserToken(
        User user,
        ServiceClient client,
        IReadOnlyCollection<string> scopes);

    AccessTokenResult IssueServiceToken(
        ServiceClient client,
        string audience,
        IReadOnlyCollection<string> scopes);
}

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
