using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.ServiceClients;

public sealed class ServiceClientTests
{
    [Fact(DisplayName = "Validar service client - deve restringir audiências e escopos")]
    public void Client_EnforcesAudienceAndScopes()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["payments-api"],
            ["payments.process", "payments.read"],
            IdentityTestData.NowUtc);

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

    [Fact(DisplayName = "Gerenciar permissões do service client - não deve criar duplicidades")]
    public void Client_ManagesPermissionsWithoutDuplicates()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["payments-api"],
            ["payments.read"],
            IdentityTestData.NowUtc);

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

    [Fact(DisplayName = "Substituir permissões do service client - não deve manter duplicidades")]
    public void Client_ReplacesPermissionsWithoutDuplicates()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["identity-api", "payments-api"],
            ["payments.read", "payments.write"],
            IdentityTestData.NowUtc);

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

    [Fact(DisplayName = "Alterar service client - deve rotacionar o segredo e permitir desativação")]
    public void Client_RotatesSecretAndCanBeDeactivated()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "old-hash",
            false,
            ["payments-api"],
            ["payments.write"],
            IdentityTestData.NowUtc);

        client.RotateSecret("new-hash");
        client.Deactivate();

        Assert.Equal("new-hash", client.SecretHash);
        Assert.False(client.IsActive);
    }

    [Fact(DisplayName = "Criar service client - deve exigir ao menos uma audience e um scope")]
    public void Create_WithoutPermissions_RejectsInvalidState()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            ServiceClient.Create(
                "orders-api",
                "Orders API",
                "secret-hash",
                false,
                [],
                ["payments.read"],
                IdentityTestData.NowUtc));
        Assert.Throws<DomainRuleViolationException>(() =>
            ServiceClient.Create(
                "orders-api",
                "Orders API",
                "secret-hash",
                false,
                ["payments-api"],
                [],
                IdentityTestData.NowUtc));
    }

    [Fact(DisplayName = "Alterar service client inativo - deve rejeitar rotação e segunda desativação")]
    public void Mutate_WhenClientIsInactive_RejectsInvalidTransitions()
    {
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "secret-hash",
            false,
            ["payments-api"],
            ["payments.read"],
            IdentityTestData.NowUtc);
        client.Deactivate();

        Assert.Throws<DomainRuleViolationException>(() =>
            client.RotateSecret("new-secret-hash"));
        Assert.Throws<DomainRuleViolationException>(client.Deactivate);
    }
}
