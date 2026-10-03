namespace FiapCloudGames.Identity.Domain.Entities;

public sealed class ServiceClientAudience
{
    private ServiceClientAudience() { }

    private ServiceClientAudience(string clientId, string audience)
    {
        ClientId = NormalizeRequired(clientId, 100, "client_id");
        Audience = NormalizeRequired(audience, 200, "audience");
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
