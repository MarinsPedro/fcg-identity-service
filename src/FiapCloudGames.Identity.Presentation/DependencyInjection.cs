using System.Text.Json;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Presentation;

/// <summary>
/// Fornece a configuração dos componentes da camada de apresentação do Identity.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra controllers e políticas de autorização do Identity.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A mesma coleção de serviços para encadeamento.</returns>
    public static IServiceCollection AddIdentityPresentation(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
                options.ModelMetadataDetailsProviders.Add(
                    new SystemTextJsonValidationMetadataProvider(
                        JsonNamingPolicy.CamelCase)))
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                IdentityPolicies.AuthLogin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.auth.login"));
            options.AddPolicy(
                IdentityPolicies.AuthRefresh,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.auth.refresh"));
            options.AddPolicy(
                IdentityPolicies.AuthRevoke,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.auth.revoke"));
            options.AddPolicy(
                IdentityPolicies.UsersCreate,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.users.create"));
            options.AddPolicy(
                IdentityPolicies.UsersRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.users.read"));
            options.AddPolicy(
                IdentityPolicies.UsersWrite,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.users.write"));
            options.AddPolicy(
                IdentityPolicies.ClientsCreate,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.clients.create"));
            options.AddPolicy(
                IdentityPolicies.ClientsRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.clients.read"));
            options.AddPolicy(
                IdentityPolicies.ClientsWrite,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.clients.write"));
            options.AddPolicy(
                IdentityPolicies.AuditRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireServiceToken()
                    .RequireClaim(FiapClaimTypes.Scope, "identity.audit.read"));
        });

        return services;
    }
}
