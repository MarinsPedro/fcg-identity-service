namespace FiapCloudGames.Identity.Presentation.Authorization;

/// <summary>
/// Define os nomes das políticas de autorização expostas pelo Identity.
/// </summary>
public static class IdentityPolicies
{
    /// <summary>Política para autenticar usuários.</summary>
    public const string AuthLogin = "identity.auth.login";

    /// <summary>Política para renovar sessões de usuários.</summary>
    public const string AuthRefresh = "identity.auth.refresh";

    /// <summary>Política para revogar sessões de usuários.</summary>
    public const string AuthRevoke = "identity.auth.revoke";

    /// <summary>Política para criar usuários.</summary>
    public const string UsersCreate = "identity.users.create";

    /// <summary>Política para consultar usuários.</summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>Política para alterar ou desativar usuários.</summary>
    public const string UsersWrite = "identity.users.write";

    /// <summary>Política para criar service clients.</summary>
    public const string ClientsCreate = "identity.clients.create";

    /// <summary>Política para consultar service clients.</summary>
    public const string ClientsRead = "identity.clients.read";

    /// <summary>Política para alterar service clients.</summary>
    public const string ClientsWrite = "identity.clients.write";

    /// <summary>Política para consultar eventos de auditoria.</summary>
    public const string AuditRead = "identity.audit.read";
}
