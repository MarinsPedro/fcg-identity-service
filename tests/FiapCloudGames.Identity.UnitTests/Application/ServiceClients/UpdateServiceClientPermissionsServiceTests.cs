using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.ServiceClients;

public sealed class UpdateServiceClientPermissionsServiceTests
{
    [Fact(DisplayName = "Atualizar permissões do client - deve substituir permissões, auditar e persistir")]
    public async Task ExecuteAsync_WithValidPermissions_UpdatesAuditsAndPersists()
    {
        var client = IdentityTestData.CreateClient(
            clientId: "orders-api",
            audiences: ["payments-api"],
            scopes: ["payments.read"]);
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(client, auditEvents, unitOfWork);

        var result = await service.ExecuteAsync(
            client.ClientId,
            new UpdateServiceClientPermissionsInput(
                ["catalog-api"],
                ["catalog.read"]),
            "web-bff",
            CancellationToken.None);

        Assert.Equal(["catalog-api"], result.AllowedAudiences);
        Assert.Equal(["catalog.read"], result.AllowedScopes);
        var auditEvent = Assert.Single(auditEvents.AddedEvents);
        Assert.Equal("service_client.permissions_changed", auditEvent.EventType);
        Assert.Equal("web-bff", auditEvent.ActorClientId);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Atualizar permissões do client - deve rejeitar cliente inativo sem auditar ou persistir")]
    public async Task ExecuteAsync_WhenClientIsInactive_ReturnsConflictWithoutEffects()
    {
        var client = IdentityTestData.CreateClient(
            clientId: "orders-api",
            active: false);
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(client, auditEvents, unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                client.ClientId,
                new UpdateServiceClientPermissionsInput(
                    ["catalog-api"],
                    ["catalog.read"]),
                "web-bff",
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.Empty(auditEvents.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static UpdateServiceClientPermissionsService CreateService(
        ServiceClient client,
        FakeSecurityAuditRepository auditEvents,
        SpyIdentityUnitOfWork unitOfWork) =>
        new(
            new FakeServiceClientRepository { Client = client },
            auditEvents,
            unitOfWork,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<UpdateServiceClientPermissionsService>.Instance);
}
