using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
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
            Events = [auditEvent]
        };
        var service = CreateService(repository);

        var result = await service.ExecuteAsync(
            CreateInput(limit: 50),
            CancellationToken.None);

        var returnedEvent = Assert.Single(result.Items);
        Assert.Equal(auditEvent.Id, returnedEvent.Id);
        Assert.Null(result.NextCursor);
        Assert.Equal(51, repository.RequestedLimit);
    }

    [Fact(DisplayName = "Obter auditoria - deve normalizar filtros e gerar cursor da próxima página")]
    public async Task ExecuteAsync_WithFiltersAndExtraEvent_ReturnsNormalizedQueryAndNextCursor()
    {
        var first = CreateAuditEvent("orders-api", IdentityTestData.NowUtc);
        var second = CreateAuditEvent("payments-api", IdentityTestData.NowUtc.AddMinutes(-1));
        var repository = new FakeSecurityAuditRepository
        {
            Events = [first, second]
        };
        var service = CreateService(repository);

        var result = await service.ExecuteAsync(
            new(
                1,
                null,
                " service_client.created ",
                " web-bff ",
                " service_client ",
                " orders-api ",
                IdentityTestData.NowUtc.AddDays(-1),
                IdentityTestData.NowUtc),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            new SecurityAuditEventCursor(first.OccurredAtUtc, first.Id),
            result.NextCursor);
        Assert.Equal("service_client.created", repository.RequestedQuery?.EventType);
        Assert.Equal("web-bff", repository.RequestedQuery?.ActorClientId);
        Assert.Equal("service_client", repository.RequestedQuery?.SubjectType);
        Assert.Equal("orders-api", repository.RequestedQuery?.SubjectId);
        Assert.Equal(2, repository.RequestedLimit);
    }

    [Theory(DisplayName = "Obter auditoria - deve rejeitar limite fora do intervalo permitido")]
    [InlineData(0)]
    [InlineData(201)]
    public async Task ExecuteAsync_WithInvalidLimit_ReturnsValidationWithoutQuerying(
        int limit)
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(CreateInput(limit: limit), CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedLimit);
    }

    [Fact(DisplayName = "Obter auditoria - deve rejeitar período invertido sem consultar repositório")]
    public async Task ExecuteAsync_WithInvertedPeriod_ReturnsValidationWithoutQuerying()
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                CreateInput(
                    occurredFromUtc: IdentityTestData.NowUtc,
                    occurredToUtc: IdentityTestData.NowUtc.AddMinutes(-1)),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedQuery);
    }

    [Fact(DisplayName = "Obter auditoria - deve rejeitar filtro acima do tamanho permitido")]
    public async Task ExecuteAsync_WithOversizedFilter_ReturnsValidationWithoutQuerying()
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);
        var input = CreateInput() with
        {
            EventType = new string('a', 101)
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(input, CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedQuery);
    }

    [Fact(DisplayName = "Obter auditoria - deve rejeitar data fora de UTC sem consultar repositório")]
    public async Task ExecuteAsync_WithNonUtcDate_ReturnsValidationWithoutQuerying()
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);
        var nonUtcDate = IdentityTestData.NowUtc.ToOffset(TimeSpan.FromHours(-3));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                CreateInput(occurredFromUtc: nonUtcDate),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedQuery);
    }

    [Fact(DisplayName = "Obter auditoria - deve rejeitar cursor inválido sem consultar repositório")]
    public async Task ExecuteAsync_WithInvalidCursor_ReturnsValidationWithoutQuerying()
    {
        var repository = new FakeSecurityAuditRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                CreateInput(after: new(IdentityTestData.NowUtc, Guid.Empty)),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
        Assert.Null(repository.RequestedQuery);
    }

    private static SecurityAuditEvent CreateAuditEvent(
        string subjectId,
        DateTimeOffset occurredAtUtc) =>
        SecurityAuditEvent.Create(
            "service_client.created",
            "web-bff",
            "service_client",
            subjectId,
            occurredAtUtc);

    private static GetSecurityAuditEventsInput CreateInput(
        int limit = 50,
        SecurityAuditEventCursor? after = null,
        DateTimeOffset? occurredFromUtc = null,
        DateTimeOffset? occurredToUtc = null) =>
        new(
            limit,
            after,
            null,
            null,
            null,
            null,
            occurredFromUtc,
            occurredToUtc);

    private static GetSecurityAuditEventsService CreateService(
        FakeSecurityAuditRepository auditEvents) =>
        new(
            auditEvents,
            NullLogger<GetSecurityAuditEventsService>.Instance);
}
