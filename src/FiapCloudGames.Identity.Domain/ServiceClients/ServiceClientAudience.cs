using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.ServiceClients;

public sealed class ServiceClientAudience
{
    public const int AudienceMaximumLength = 200;

    private ServiceClientAudience() { }

    private ServiceClientAudience(string clientId, string audience)
    {
        ClientId = NormalizeRequired(
            clientId,
            ServiceClient.ClientIdMaximumLength,
            "client_id");
        Audience = NormalizeRequired(
            audience,
            AudienceMaximumLength,
            "audience");
    }

    public string ClientId { get; private set; } = string.Empty;
    public string Audience { get; private set; } = string.Empty;

    internal static ServiceClientAudience Create(string clientId, string audience) =>
        new(clientId, audience);

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string field) =>
        value?.Trim() is { Length: > 0 } normalized &&
        normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
