using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Infrastructure;
using FiapCloudGames.Identity.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.AddFiapServiceDefaults();
builder.Services.AddFiapApiDefaults();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddIdentityPresentation();

var app = builder.Build();

app.UseFiapApiDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapFiapHealthChecks().AllowAnonymous();
app.MapControllers();

await app.RunAsync();

public partial class Program;
