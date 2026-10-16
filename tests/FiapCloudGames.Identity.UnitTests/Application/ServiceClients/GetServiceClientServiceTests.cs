using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Get;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.ServiceClients;

public sealed class GetServiceClientServiceTests
{
    [Fact(DisplayName = "Obter service client - deve retornar cliente existente sem segredo")]
    public async Task ExecuteAsync_WhenClientExists_ReturnsPublicData()
    {
        var client = IdentityTestData.CreateClient();
        var service = CreateService(
            new FakeServiceClientRepository { Client = client });

        var result = await service.ExecuteAsync(
            client.ClientId,
            CancellationToken.None);

        Assert.Equal(client.ClientId, result.ClientId);
        Assert.DoesNotContain(
            result.GetType().GetProperties(),
            property => property.Name.Contains("Secret", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Obter service client - deve retornar não encontrado para cliente inexistente")]
    public async Task ExecuteAsync_WhenClientDoesNotExist_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService(new FakeServiceClientRepository()).ExecuteAsync(
                "missing-api",
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.NotFound, exception.Category);
    }

    private static GetServiceClientService CreateService(
        FakeServiceClientRepository clients) =>
        new(clients, NullLogger<GetServiceClientService>.Instance);
}
