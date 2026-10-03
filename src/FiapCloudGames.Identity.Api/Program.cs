using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Api.Configuration;
using FiapCloudGames.Identity.Infrastructure;
using FiapCloudGames.Identity.Presentation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddFiapServiceDefaults();
builder.Services.AddFiapApiDefaults();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddIdentityPresentation();
builder.Services.AddIdentityOpenApi();

var app = builder.Build();

app.UseFiapApiDefaults();
app.UseIdentityOpenApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapFiapHealthChecks().AllowAnonymous();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
        AllowCachingResponses = false
    }).AllowAnonymous();
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        AllowCachingResponses = false
    }).AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program;
