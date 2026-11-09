namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class InitialServiceClientsOptions
{
    public const string SectionName = "InitialServiceClients";

    public List<InitialServiceClientOptions> Clients { get; set; } = [];
}

internal sealed class InitialServiceClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public bool CanIssueUserTokens { get; set; }
    public List<string> AllowedAudiences { get; set; } = [];
    public List<string> AllowedScopes { get; set; } = [];
}
