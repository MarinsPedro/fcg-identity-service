namespace FiapCloudGames.Identity.Presentation.Authorization;

public static class IdentityPolicies
{
    public const string UsersCreate = "identity.users.create";
    public const string UsersRead = "identity.users.read";
    public const string UsersWrite = "identity.users.write";
    public const string ClientsCreate = "identity.clients.create";
    public const string ClientsRead = "identity.clients.read";
    public const string ClientsWrite = "identity.clients.write";
    public const string AuditRead = "identity.audit.read";
}
