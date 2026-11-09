using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.ServiceClients;

public sealed class ServiceClient
{
    public const int ClientIdMaximumLength = 100;
    public const int DisplayNameMaximumLength = 200;
    public const int SecretHashMaximumLength = 500;

    private readonly List<ServiceClientAudience> allowedAudiences = [];
    private readonly List<ServiceClientScope> allowedScopes = [];

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
        ClientId = NormalizeRequired(
            clientId,
            ClientIdMaximumLength,
            "client_id");
        DisplayName = NormalizeRequired(
            displayName,
            DisplayNameMaximumLength,
            "nome do client");
        SecretHash = NormalizeRequired(
            secretHash,
            SecretHashMaximumLength,
            "hash do segredo");
        CanIssueUserTokens = canIssueUserTokens;
        AddAudiences(allowedAudiences);
        AddScopes(allowedScopes);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string ClientId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public bool CanIssueUserTokens { get; private set; }
    public IReadOnlyCollection<ServiceClientAudience> AllowedAudiences => allowedAudiences;
    public IReadOnlyCollection<ServiceClientScope> AllowedScopes => allowedScopes;
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
        allowedAudiences.Any(allowed =>
            string.Equals(allowed.Audience, audience?.Trim(), StringComparison.Ordinal));

    public bool AllowsScopes(IEnumerable<string> scopes)
    {
        var allowed = allowedScopes
            .Select(registered => registered.Scope)
            .ToHashSet(StringComparer.Ordinal);
        return scopes.All(scope => allowed.Contains(scope.Trim()));
    }

    public void AllowAudience(string audience)
    {
        var permission = ServiceClientAudience.Create(ClientId, audience);
        if (allowedAudiences.All(current => current.Audience != permission.Audience))
        {
            allowedAudiences.Add(permission);
        }
    }

    public bool RemoveAudience(string audience) =>
        allowedAudiences.RemoveAll(current =>
            string.Equals(current.Audience, audience?.Trim(), StringComparison.Ordinal)) > 0;

    public void GrantScope(string scope)
    {
        var permission = ServiceClientScope.Create(ClientId, scope);
        if (allowedScopes.All(current => current.Scope != permission.Scope))
        {
            allowedScopes.Add(permission);
        }
    }

    public bool RevokeScope(string scope) =>
        allowedScopes.RemoveAll(current =>
            string.Equals(current.Scope, scope?.Trim(), StringComparison.Ordinal)) > 0;

    public void ReplacePermissions(
        IEnumerable<string> audiences,
        IEnumerable<string> scopes)
    {
        var normalizedAudiences = NormalizeList(audiences, "audiences");
        var normalizedScopes = NormalizeList(scopes, "scopes");
        var requestedAudiences = normalizedAudiences.ToHashSet(StringComparer.Ordinal);
        var requestedScopes = normalizedScopes.ToHashSet(StringComparer.Ordinal);

        allowedAudiences.RemoveAll(current =>
            !requestedAudiences.Contains(current.Audience));
        allowedScopes.RemoveAll(current =>
            !requestedScopes.Contains(current.Scope));
        foreach (var audience in normalizedAudiences)
        {
            AllowAudience(audience);
        }

        foreach (var scope in normalizedScopes)
        {
            GrantScope(scope);
        }
    }

    public void ApplyProvisioningConfiguration(
        string displayName,
        string secretHash,
        bool canIssueUserTokens,
        IEnumerable<string> audiences,
        IEnumerable<string> scopes)
    {
        DisplayName = NormalizeRequired(
            displayName,
            DisplayNameMaximumLength,
            "nome do client");
        SecretHash = NormalizeRequired(
            secretHash,
            SecretHashMaximumLength,
            "hash do segredo");
        CanIssueUserTokens = canIssueUserTokens;
        IsActive = true;
        ReplacePermissions(audiences, scopes);
    }

    public void RotateSecret(string secretHash)
    {
        if (!IsActive)
        {
            throw new DomainRuleViolationException(
                "Não é possível rotacionar o segredo de um client inativo.");
        }

        SecretHash = NormalizeRequired(
            secretHash,
            SecretHashMaximumLength,
            "hash do segredo");
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainRuleViolationException("O client já está inativo.");
        }

        IsActive = false;
    }

    private void AddAudiences(IEnumerable<string> audiences)
    {
        foreach (var audience in NormalizeList(audiences, "audiences"))
        {
            AllowAudience(audience);
        }
    }

    private void AddScopes(IEnumerable<string> scopes)
    {
        foreach (var scope in NormalizeList(scopes, "scopes"))
        {
            GrantScope(scope);
        }
    }

    private static string[] NormalizeList(IEnumerable<string> values, string name)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return normalized.Length > 0
            ? normalized
            : throw new DomainRuleViolationException(
                $"Informe ao menos um valor para {name}.");
    }

    private static string NormalizeRequired(string? value, int maximumLength, string field) =>
        value?.Trim() is { Length: > 0 } normalized && normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
