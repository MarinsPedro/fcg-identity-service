namespace FiapCloudGames.Identity.Domain.Entities;

public sealed class ServiceClientScope
{
    private ServiceClientScope() { }

    private ServiceClientScope(string clientId, string scope)
    {
        ClientId = NormalizeRequired(clientId, 100, "client_id");
        Scope = NormalizeRequired(scope, 200, "scope");
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
