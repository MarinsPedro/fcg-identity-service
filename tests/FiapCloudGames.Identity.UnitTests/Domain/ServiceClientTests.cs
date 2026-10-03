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

    [Fact]
    public void Client_ReplacesPermissionsWithoutDuplicates()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["identity-api", "payments-api"],
            ["payments.read", "payments.write"],
            DateTimeOffset.UtcNow);

        client.ReplacePermissions(
            ["payments-api", "catalog-api", "catalog-api"],
            ["catalog.read", "catalog.read"]);

        Assert.Equal(
            ["catalog-api", "payments-api"],
            client.AllowedAudiences.Select(item => item.Audience).Order());
        Assert.Equal(
            ["catalog.read"],
            client.AllowedScopes.Select(item => item.Scope));
    }

    [Fact]
    public void Client_RotatesSecretAndCanBeDeactivated()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "old-hash",
            false,
            ["payments-api"],
            ["payments.write"],
            DateTimeOffset.UtcNow);

        client.RotateSecret("new-hash");
        client.Deactivate();

        Assert.Equal("new-hash", client.SecretHash);
        Assert.False(client.IsActive);
    }
}
