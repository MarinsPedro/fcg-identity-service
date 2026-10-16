using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.ServiceClients.Deactivate;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.ServiceClients;

public sealed class DeactivateServiceClientServiceTests
{
    [Fact(DisplayName = "Desativar service client - deve desativar, auditar e persistir")]
    public async Task ExecuteAsync_WhenClientIsActive_DeactivatesAuditsAndPersists()
    {
        var client = IdentityTestData.CreateClient(clientId: "orders-api");
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(client, auditEvents, unitOfWork);

        await service.ExecuteAsync(
            client.ClientId,
            "web-bff",
            CancellationToken.None);

        Assert.False(client.IsActive);
        var auditEvent = Assert.Single(auditEvents.AddedEvents);
        Assert.Equal("service_client.deactivated", auditEvent.EventType);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Desativar service client - deve impedir que o client desative a si próprio")]
    public async Task ExecuteAsync_WhenActorIsTarget_ReturnsConflictWithoutEffects()
    {
        var client = IdentityTestData.CreateClient(clientId: "web-bff");
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(client, auditEvents, unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                client.ClientId,
                client.ClientId,
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.True(client.IsActive);
        Assert.Empty(auditEvents.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static DeactivateServiceClientService CreateService(
        ServiceClient client,
        FakeSecurityAuditRepository auditEvents,
        SpyIdentityUnitOfWork unitOfWork) =>
        new(
            new FakeServiceClientRepository { Client = client },
            auditEvents,
            unitOfWork,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<DeactivateServiceClientService>.Instance);
}
