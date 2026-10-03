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
        Assert.Equal(
            ["payments-api"],
            client.AllowedAudiences.Select(item => item.Audience));
        Assert.Equal(
            ["payments.process", "payments.read"],
            client.AllowedScopes.Select(item => item.Scope));
    }

    [Fact]
    public void Client_ManagesPermissionsWithoutDuplicates()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["payments-api"],
            ["payments.read"],
            DateTimeOffset.UtcNow);

        client.AllowAudience("payments-api");
        client.AllowAudience("catalog-api");
        client.GrantScope("payments.read");
        client.GrantScope("payments.process");

        Assert.Equal(2, client.AllowedAudiences.Count);
        Assert.Equal(2, client.AllowedScopes.Count);
        Assert.True(client.RemoveAudience("catalog-api"));
        Assert.True(client.RevokeScope("payments.process"));
        Assert.False(client.RemoveAudience("unknown-api"));
        Assert.False(client.RevokeScope("unknown.scope"));
    }
}
