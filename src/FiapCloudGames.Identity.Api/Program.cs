using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Api;
using FiapCloudGames.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddFiapServiceDefaults();
builder.Services.AddFiapApiDefaults();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddAuthorization(options =>
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

var app = builder.Build();

app.UseFiapApiDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapFiapHealthChecks().AllowAnonymous();
app.MapIdentityEndpoints();

await app.RunAsync();

public partial class Program;
