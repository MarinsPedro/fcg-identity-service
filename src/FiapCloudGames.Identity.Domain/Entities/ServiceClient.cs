namespace FiapCloudGames.Identity.Domain.Entities;

public sealed class ServiceClient
{
    private ServiceClient() { }

    private ServiceClient(
        string clientId,
        string displayName,
        string secretHash,
        bool canIssueUserTokens,
        IEnumerable<string> allowedAudiences,
        IEnumerable<string> allowedScopes,
        DateTimeOffset createdAtUtc)
    {
        ClientId = NormalizeRequired(clientId, 100, "client_id");
        DisplayName = NormalizeRequired(displayName, 200, "nome do client");
        SecretHash = NormalizeRequired(secretHash, 500, "hash do segredo");
        CanIssueUserTokens = canIssueUserTokens;
        AllowedAudiences = NormalizeList(allowedAudiences, "audiences");
        AllowedScopes = NormalizeList(allowedScopes, "scopes");
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string ClientId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public bool CanIssueUserTokens { get; private set; }
    public string AllowedAudiences { get; private set; } = string.Empty;
    public string AllowedScopes { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ServiceClient Create(
        string clientId,
        string displayName,
        string secretHash,
        bool canIssueUserTokens,
        IEnumerable<string> allowedAudiences,
        IEnumerable<string> allowedScopes,
        DateTimeOffset createdAtUtc) =>
        new(
            clientId,
            displayName,
            secretHash,
            canIssueUserTokens,
            allowedAudiences,
            allowedScopes,
            createdAtUtc);

    public bool AllowsAudience(string audience) =>
        Split(AllowedAudiences).Contains(audience, StringComparer.Ordinal);

    public bool AllowsScopes(IEnumerable<string> scopes)
    {
        var allowed = Split(AllowedScopes).ToHashSet(StringComparer.Ordinal);
        return scopes.All(allowed.Contains);
    }

    private static string NormalizeList(IEnumerable<string> values, string name)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return normalized.Length > 0
            ? string.Join(' ', normalized)
            : throw new DomainRuleViolationException($"Informe ao menos um valor para {name}.");
    }

    private static string[] Split(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string NormalizeRequired(string? value, int maximumLength, string field) =>
        value?.Trim() is { Length: > 0 } normalized && normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
