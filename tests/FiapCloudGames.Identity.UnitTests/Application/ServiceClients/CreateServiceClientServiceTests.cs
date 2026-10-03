using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Create;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Application.ServiceClients;

public sealed class CreateServiceClientServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidInput_CreatesClientAndAuditEvent()
    {
        var clients = new FakeServiceClientRepository();
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var secretGenerator = new StubClientSecretGenerator();
        var hasher = new StubPasswordHasher();
        var service = new CreateServiceClientService(
            clients,
            auditEvents,
            unitOfWork,
            secretGenerator,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc));

        var result = await service.ExecuteAsync(
            new CreateServiceClientInput(
                " orders-api ",
                "Orders API",
                false,
                ["payments-api"],
                ["payments.write"]),
            "web-bff",
            CancellationToken.None);

        Assert.NotNull(clients.AddedClient);
        Assert.Equal("orders-api", clients.AddedClient.ClientId);
        Assert.Equal("secret-hash", clients.AddedClient.SecretHash);
        Assert.Equal("generated-secret", result.ClientSecret);
        Assert.Single(auditEvents.AddedEvents);
        Assert.Equal("web-bff", auditEvents.AddedEvents[0].ActorClientId);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClientAlreadyExists_ReturnsConflictWithoutPersisting()
    {
        var clients = new FakeServiceClientRepository
        {
            Client = CreateClient("orders-api")
        };
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var secretGenerator = new StubClientSecretGenerator();
        var hasher = new StubPasswordHasher();
        var service = new CreateServiceClientService(
            clients,
            auditEvents,
            unitOfWork,
            secretGenerator,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new CreateServiceClientInput(
                    "orders-api",
                    "Orders API",
                    false,
                    ["payments-api"],
                    ["payments.write"]),
                "web-bff",
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.Null(clients.AddedClient);
        Assert.Empty(auditEvents.AddedEvents);
        Assert.Equal(0, secretGenerator.GenerateCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static ServiceClient CreateClient(string clientId) =>
        ServiceClient.Create(
            clientId,
            "Orders API",
            "secret-hash",
            false,
            ["payments-api"],
            ["payments.write"],
            IdentityTestData.NowUtc);
}
