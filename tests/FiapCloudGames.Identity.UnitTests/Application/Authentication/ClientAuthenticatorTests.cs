using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class ClientAuthenticatorTests
{
    [Fact(DisplayName = "Autenticar service client - deve retornar cliente com credenciais válidas")]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsClient()
    {
        var client = IdentityTestData.CreateClient();
        var clients = new FakeServiceClientRepository { Client = client };
        var hasher = new StubPasswordHasher { VerifyResult = true };
        var authenticator = new ClientAuthenticator(
            clients,
            hasher,
            NullLogger<ClientAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync(
            "web-bff",
            "valid-secret",
            CancellationToken.None);

        Assert.Same(client, result);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    [Theory(DisplayName = "Autenticar service client - deve rejeitar credenciais inválidas sem revelar a causa")]
    [InlineData(null, "secret")]
    [InlineData("web-bff", null)]
    [InlineData("missing-client", "secret")]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsAuthenticationError(
        string? clientId,
        string? secret)
    {
        var clients = new FakeServiceClientRepository
        {
            Client = IdentityTestData.CreateClient()
        };
        var authenticator = new ClientAuthenticator(
            clients,
            new StubPasswordHasher(),
            NullLogger<ClientAuthenticator>.Instance);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            authenticator.AuthenticateAsync(
                clientId,
                secret,
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Authentication, exception.Category);
        Assert.Equal("Client inválido.", exception.Message);
    }
}
