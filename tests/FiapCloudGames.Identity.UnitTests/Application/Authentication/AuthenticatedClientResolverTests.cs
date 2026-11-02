using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class AuthenticatedClientResolverTests
{
    [Fact(DisplayName = "Resolver service client autenticado - deve retornar cliente ativo")]
    public async Task ResolveAsync_WhenClientIsActive_ReturnsClient()
    {
        var client = IdentityTestData.CreateClient();
        var resolver = CreateResolver(
            new FakeServiceClientRepository { Client = client });

        var result = await resolver.ResolveAsync(
            client.ClientId,
            CancellationToken.None);

        Assert.Same(client, result);
    }

    [Theory(DisplayName = "Resolver service client autenticado - deve rejeitar cliente ausente ou inativo")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveAsync_WhenClientIsUnavailable_ReturnsForbidden(
        bool clientExists)
    {
        var repository = new FakeServiceClientRepository
        {
            Client = clientExists
                ? IdentityTestData.CreateClient(active: false)
                : null
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateResolver(repository).ResolveAsync(
                "web-bff",
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Forbidden, exception.Category);
    }

    private static AuthenticatedClientResolver CreateResolver(
        FakeServiceClientRepository clients) =>
        new(
            clients,
            NullLogger<AuthenticatedClientResolver>.Instance);
}
