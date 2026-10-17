using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.ServiceClients;

public sealed class ServiceClientScope
{
    public const int ScopeMaximumLength = 200;

    private ServiceClientScope() { }

    private ServiceClientScope(string clientId, string scope)
    {
        ClientId = NormalizeRequired(
            clientId,
            ServiceClient.ClientIdMaximumLength,
            "client_id");
        Scope = NormalizeRequired(
            scope,
            ScopeMaximumLength,
            "scope");
    }

    public string ClientId { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;

    internal static ServiceClientScope Create(string clientId, string scope) =>
        new(clientId, scope);

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string field) =>
        value?.Trim() is { Length: > 0 } normalized &&
        normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
