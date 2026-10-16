using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class ClientCredentialsServiceTests
{
    [Fact(DisplayName = "Emitir token de serviço - deve normalizar scopes e emitir token autorizado")]
    public async Task ExecuteAsync_WithAllowedAudienceAndScopes_IssuesToken()
    {
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            IdentityTestData.CreateClient(
                clientId: "orders-api",
                audiences: ["payments-api"],
                scopes: ["payments.read", "payments.write"]),
            issuer);

        var result = await service.ExecuteAsync(
            new ClientCredentialsInput(
                "client_credentials",
                "orders-api",
                "valid-secret",
                "payments-api",
                "payments.write payments.read payments.write"),
            CancellationToken.None);

        Assert.Equal("service-access-token", result.AccessToken);
        Assert.Equal("payments.read payments.write", result.Scope);
        Assert.Equal(300, result.ExpiresIn);
        Assert.Equal(1, issuer.ServiceTokenCalls);
        Assert.Equal("payments-api", issuer.RequestedAudience);
        Assert.Equal(["payments.read", "payments.write"], issuer.RequestedScopes);
    }

    [Fact(DisplayName = "Emitir token de serviço - não deve emitir quando audience ou scopes não forem permitidos")]
    public async Task ExecuteAsync_WithForbiddenPermissions_ReturnsForbiddenWithoutIssuingToken()
    {
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            IdentityTestData.CreateClient(
                clientId: "orders-api",
                audiences: ["payments-api"],
                scopes: ["payments.read"]),
            issuer);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new ClientCredentialsInput(
                    "client_credentials",
                    "orders-api",
                    "valid-secret",
                    "catalog-api",
                    "catalog.write"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Forbidden, exception.Category);
        Assert.Equal(0, issuer.ServiceTokenCalls);
    }

    [Fact(DisplayName = "Emitir token de serviço - deve rejeitar grant type não suportado antes da autenticação")]
    public async Task ExecuteAsync_WithUnsupportedGrantType_ReturnsValidationWithoutAuthenticating()
    {
        var clients = new FakeServiceClientRepository();
        var service = CreateService(clients, new StubTokenIssuer());

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new ClientCredentialsInput(
                    "password",
                    "orders-api",
                    "secret",
                    "payments-api",
                    "payments.read"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Equal(0, clients.FindByIdCalls);
    }

    private static ClientCredentialsService CreateService(
        ServiceClient client,
        StubTokenIssuer issuer) =>
        CreateService(
            new FakeServiceClientRepository { Client = client },
            issuer);

    private static ClientCredentialsService CreateService(
        FakeServiceClientRepository clients,
        StubTokenIssuer issuer)
    {
        var hasher = new StubPasswordHasher { VerifyResult = true };
        var authenticator = new ClientAuthenticator(
            clients,
            hasher,
            NullLogger<ClientAuthenticator>.Instance);
        return new ClientCredentialsService(
            authenticator,
            issuer,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<ClientCredentialsService>.Instance);
    }
}
