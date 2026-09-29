using FiapCloudGames.BuildingBlocks.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityPresentation(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        services.AddAuthorization(options =>
        {
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
        });

        return services;
    }
}
