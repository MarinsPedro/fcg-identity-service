using System.Net;
using FiapCloudGames.Identity.IntegrationTests.Support;

namespace FiapCloudGames.Identity.IntegrationTests.Host;

public sealed class HealthEndpointTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Consultar health checks - deve reportar a aplicação como ativa e pronta")]
    public async Task HealthEndpoints_WhenApplicationIsHealthy_ReportLiveAndReady()
    {
        using var client = factory.CreateClient();

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }
}
