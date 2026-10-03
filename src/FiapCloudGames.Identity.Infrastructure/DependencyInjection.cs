using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Application;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.Infrastructure.Authentication;
using FiapCloudGames.Identity.Infrastructure.Health;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;
using FiapCloudGames.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FiapCloudGames.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "A connection string 'Database' não foi configurada.");

        services.AddIdentityApplication();
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer é obrigatório.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.KeyId),
                "Jwt:KeyId é obrigatório.")
            .Validate(
                options => options.UserTokenLifetime > TimeSpan.Zero &&
                    options.ServiceTokenLifetime > TimeSpan.Zero &&
                    options.RefreshTokenLifetime > TimeSpan.Zero,
                "Os tempos de vida dos tokens devem ser maiores que zero.")
            .ValidateOnStart();
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IIdentityUnitOfWork>(provider =>
            provider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceClientRepository, ServiceClientRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IClientSecretGenerator, ClientSecretGenerator>();
        services.AddSingleton<IRefreshTokenProtector, RefreshTokenProtector>();
        services.AddSingleton<RsaSigningKeyProvider>();
        services.AddSingleton<ITokenDiscoveryProvider>(provider =>
            provider.GetRequiredService<RsaSigningKeyProvider>());
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddHealthChecks()
            .AddCheck<IdentityDatabaseHealthCheck>(
                "identity-database",
                tags: ["ready"]);

        AddAuthentication(services);
        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<RsaSigningKeyProvider, Microsoft.Extensions.Options.IOptions<JwtOptions>>(
                (options, provider, jwtOptions) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = "identity-api",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = provider.SecurityKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = FiapClaimTypes.Subject,
                    RoleClaimType = FiapClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}
