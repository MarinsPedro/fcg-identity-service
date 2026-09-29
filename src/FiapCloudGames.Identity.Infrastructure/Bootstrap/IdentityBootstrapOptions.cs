namespace FiapCloudGames.Identity.Infrastructure.Bootstrap;

public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "Identity";

    public List<BootstrapClientOptions> BootstrapClients { get; set; } = [];
}

public sealed class BootstrapClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool CanIssueUserTokens { get; set; }
    public List<string> AllowedAudiences { get; set; } = [];
    public List<string> AllowedScopes { get; set; } = [];
}
