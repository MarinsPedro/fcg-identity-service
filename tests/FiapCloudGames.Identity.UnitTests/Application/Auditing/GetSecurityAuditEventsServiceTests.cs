using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Auditing.GetSecurityAuditEvents;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Auditing;

public sealed class GetSecurityAuditEventsServiceTests
{
    [Fact(DisplayName = "Obter auditoria - deve retornar eventos recentes respeitando o limite")]
    public async Task ExecuteAsync_WithValidLimit_ReturnsRecentEvents()
    {
        var auditEvent = SecurityAuditEvent.Create(
            "service_client.created",
            "web-bff",
            "service_client",
            "orders-api",
            IdentityTestData.NowUtc);
        var repository = new FakeSecurityAuditRepository
        {
            RecentEvents = [auditEvent]
        };
        var service = CreateService(repository);

        var result = await service.ExecuteAsync(50, CancellationToken.None);

        var returnedEvent = Assert.Single(result);
        Assert.Equal(auditEvent.Id, returnedEvent.Id);
        Assert.Equal(50, repository.RequestedTake);
    }

    [Theory(DisplayName = "Obter auditoria - deve rejeitar limite fora do intervalo permitido")]
    [InlineData(0)]
    [InlineData(201)]
    public async Task ExecuteAsync_WithInvalidLimit_ReturnsValidationWithoutQuerying(
        int take)
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(take, CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedTake);
    }

    private static GetSecurityAuditEventsService CreateService(
        FakeSecurityAuditRepository auditEvents) =>
        new(
            auditEvents,
            NullLogger<GetSecurityAuditEventsService>.Instance);
}
