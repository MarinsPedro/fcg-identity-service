using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.Api.Configuration;
using FiapCloudGames.Identity.Infrastructure;
using FiapCloudGames.Identity.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.AddFiapServiceDefaults();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
});
builder.Services.AddFiapApiDefaults();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddIdentityPresentation();
builder.Services.AddIdentityOpenApi();

var app = builder.Build();

app.UseFiapApiDefaults();
app.UseIdentityOpenApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapFiapHealthEndpoints();
app.MapControllers();

await app.RunAsync();

public partial class Program;
