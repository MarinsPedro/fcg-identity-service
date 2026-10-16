using System.IdentityModel.Tokens.Jwt;
using FiapCloudGames.BuildingBlocks.Authentication;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.IntegrationTests.Components.Authentication;

public sealed class JwtTokenIssuerTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Emitir token de serviço - deve usar o cliente como subject")]
    public void IssueServiceToken_UsesClientAsSubject()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            KeyId = "test-key",
            AllowEphemeralDevelopmentKey = true
        });
        using var keys = new RsaSigningKeyProvider(options, new TestEnvironment());
        var issuer = new JwtTokenIssuer(
            keys,
            options,
            new FixedTimeProvider(NowUtc));
        var client = ServiceClient.Create(
            "orders-api",
            "Orders API",
            "hash",
            false,
            ["payments-api"],
            ["payments.process"],
            NowUtc);

        var generated = issuer.IssueServiceToken(
            client,
            "payments-api",
            ["payments.process"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.AccessToken);

        Assert.Equal("orders-api", token.Subject);
        Assert.Equal("orders-api", token.Claims.Single(c => c.Type == FiapClaimTypes.ClientId).Value);
        Assert.Equal(FiapTokenUses.Service, token.Claims.Single(c => c.Type == FiapClaimTypes.TokenUse).Value);
        Assert.Equal("payments-api", token.Audiences.Single());
        Assert.Equal("RS256", token.Header.Alg);
        Assert.Equal("test-key", token.Header.Kid);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Equal(NowUtc.AddMinutes(5).UtcDateTime, token.ValidTo);
        Assert.Equal(
            ["payments.process"],
            token.Claims
                .Where(claim => claim.Type == FiapClaimTypes.Scope)
                .Select(claim => claim.Value));
    }

    [Fact(DisplayName = "Emitir token de usuário - deve usar o usuário como subject e o edge como audience")]
    public void IssueUserToken_UsesUserAsSubjectAndEdgeAsAudience()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            KeyId = "test-key",
            AllowEphemeralDevelopmentKey = true
        });
        using var keys = new RsaSigningKeyProvider(options, new TestEnvironment());
        var issuer = new JwtTokenIssuer(
            keys,
            options,
            new FixedTimeProvider(NowUtc));
        var user = User.Create(
            "Test User",
            Email.Create("test@example.com"),
            "hash",
            NowUtc);
        var client = ServiceClient.Create(
            "web-bff",
            "Web BFF",
            "hash",
            true,
            ["web-bff"],
            ["store.access"],
            NowUtc);

        var generated = issuer.IssueUserToken(user, client, ["store.access"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.AccessToken);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(FiapTokenUses.User, token.Claims.Single(c => c.Type == FiapClaimTypes.TokenUse).Value);
        Assert.Equal("web-bff", token.Audiences.Single());
        Assert.Equal(NowUtc.AddMinutes(15).UtcDateTime, token.ValidTo);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }
}
