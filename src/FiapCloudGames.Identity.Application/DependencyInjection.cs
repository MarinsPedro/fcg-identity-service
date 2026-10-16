using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Create;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Deactivate;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Get;
using FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;
using FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;
using FiapCloudGames.Identity.Application.Features.Users.Create;
using FiapCloudGames.Identity.Application.Features.Users.Deactivate;
using FiapCloudGames.Identity.Application.Features.Users.Get;
using FiapCloudGames.Identity.Application.Features.Users.Update;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientAuthenticator>();
        services.AddScoped<AuthenticatedClientResolver>();
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
