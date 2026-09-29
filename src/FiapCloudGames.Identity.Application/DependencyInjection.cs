using FiapCloudGames.Identity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientAuthenticator>();
        services.AddScoped<RegisterUserService>();
        services.AddScoped<LoginService>();
        services.AddScoped<ClientCredentialsService>();
        services.AddScoped<RefreshUserTokenService>();
        services.AddScoped<RevokeRefreshTokenService>();
        services.AddScoped<GetUserService>();
        services.AddScoped<UpdateUserService>();
        services.AddScoped<DeactivateUserService>();
        return services;
    }
}
