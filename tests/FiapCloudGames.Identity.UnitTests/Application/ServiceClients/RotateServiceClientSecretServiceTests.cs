using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.ServiceClients.RotateSecret;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.ServiceClients;

public sealed class RotateServiceClientSecretServiceTests
{
    [Fact(DisplayName = "Rotacionar segredo - deve atualizar o cliente e registrar auditoria")]
    public async Task ExecuteAsync_WhenClientExists_RotatesSecretAndCreatesAuditEvent()
    {
        var client = CreateClient();
        var clients = new FakeServiceClientRepository { Client = client };
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var secretGenerator = new StubClientSecretGenerator();
        var hasher = new StubPasswordHasher();
        var service = new RotateServiceClientSecretService(
            clients,
            auditEvents,
            unitOfWork,
            secretGenerator,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<RotateServiceClientSecretService>.Instance);

        var result = await service.ExecuteAsync(
            client.ClientId,
            "web-bff",
            CancellationToken.None);

        Assert.Equal("secret-hash", client.SecretHash);
        Assert.Equal("generated-secret", result.ClientSecret);
        Assert.Single(auditEvents.AddedEvents);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Rotacionar segredo - deve retornar não encontrado sem gerar segredo quando o cliente não existir")]
    public async Task ExecuteAsync_WhenClientDoesNotExist_ReturnsNotFoundWithoutGeneratingSecret()
    {
        var clients = new FakeServiceClientRepository();
        var auditEvents = new FakeSecurityAuditRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var secretGenerator = new StubClientSecretGenerator();
        var hasher = new StubPasswordHasher();
        var service = new RotateServiceClientSecretService(
            clients,
            auditEvents,
            unitOfWork,
            secretGenerator,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<RotateServiceClientSecretService>.Instance);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                "missing-api",
                "web-bff",
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.NotFound, exception.Category);
        Assert.Equal(0, secretGenerator.GenerateCalls);
        Assert.Empty(auditEvents.AddedEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static ServiceClient CreateClient() =>
        ServiceClient.Create(
            "orders-api",
            "Orders API",
            "old-secret-hash",
            false,
            ["payments-api"],
            ["payments.write"],
            IdentityTestData.NowUtc);
}
