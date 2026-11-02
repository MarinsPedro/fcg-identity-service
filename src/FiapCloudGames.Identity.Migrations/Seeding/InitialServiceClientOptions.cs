namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class InitialServiceClientOptions
{
    public const string SectionName = "InitialServiceClient";
    public const string RequiredClientId = "web-bff";

    public string ClientId { get; set; } = RequiredClientId;
    public string DisplayName { get; set; } = "Web BFF";
    public string? Secret { get; set; }
    public bool CanIssueUserTokens { get; set; } = true;
    public List<string> AllowedAudiences { get; set; } =
        [RequiredClientId, "identity-api"];
    public List<string> AllowedScopes { get; set; } =
    [
        "store.access",
        "identity.auth.login",
        "identity.auth.refresh",
        "identity.auth.revoke",
        "identity.users.create",
        "identity.users.read",
        "identity.users.write",
        "identity.clients.create",
        "identity.clients.read",
        "identity.clients.write",
        "identity.audit.read"
    ];
}
