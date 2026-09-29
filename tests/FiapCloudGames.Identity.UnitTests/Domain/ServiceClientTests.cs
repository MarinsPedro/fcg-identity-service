using FiapCloudGames.Identity.Domain.Entities;

namespace FiapCloudGames.Identity.UnitTests.Domain;

public sealed class ServiceClientTests
{
    [Fact]
    public void Client_EnforcesAudienceAndScopes()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["payments-api"],
            ["payments.process", "payments.read"],
            DateTimeOffset.UtcNow);

        Assert.True(client.AllowsAudience("payments-api"));
        Assert.False(client.AllowsAudience("catalog-api"));
        Assert.True(client.AllowsScopes(["payments.process"]));
        Assert.False(client.AllowsScopes(["payments.admin"]));
    }
}
