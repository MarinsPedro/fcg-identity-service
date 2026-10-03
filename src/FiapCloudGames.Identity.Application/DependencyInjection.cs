using FiapCloudGames.Identity.Application.Features.Authentication;
using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.Application.Features.Audit.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Application.Features.Clients.CreateClient;
using FiapCloudGames.Identity.Application.Features.Clients.DeactivateClient;
using FiapCloudGames.Identity.Application.Features.Clients.GetClient;
using FiapCloudGames.Identity.Application.Features.Clients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.Clients.UpdatePermissions;
using FiapCloudGames.Identity.Application.Features.Users.CreateUser;
using FiapCloudGames.Identity.Application.Features.Users.DeactivateUser;
using FiapCloudGames.Identity.Application.Features.Users.GetUser;
using FiapCloudGames.Identity.Application.Features.Users.UpdateUser;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientAuthenticator>();
        services.AddScoped<CreateUserService>();
        services.AddScoped<LoginService>();
        services.AddScoped<ClientCredentialsService>();
        services.AddScoped<RefreshUserTokenService>();
        services.AddScoped<RevokeRefreshTokenService>();
        services.AddScoped<GetUserService>();
        services.AddScoped<UpdateUserService>();
        services.AddScoped<DeactivateUserService>();
        services.AddScoped<CreateServiceClientService>();
        services.AddScoped<GetServiceClientService>();
        services.AddScoped<UpdateServiceClientPermissionsService>();
        services.AddScoped<RotateServiceClientSecretService>();
        services.AddScoped<DeactivateServiceClientService>();
        services.AddScoped<GetSecurityAuditEventsService>();
        return services;
    }
}
