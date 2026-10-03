using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Abstractions.Security;

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
