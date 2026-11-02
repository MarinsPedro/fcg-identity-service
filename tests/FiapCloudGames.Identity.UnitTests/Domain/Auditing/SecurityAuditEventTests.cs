using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.Auditing;

public sealed class SecurityAuditEventTests
{
    [Fact(DisplayName = "Criar evento de auditoria - deve normalizar e preservar dados válidos")]
    public void Create_WithValidData_CreatesNormalizedEvent()
    {
        var auditEvent = SecurityAuditEvent.Create(
            " service_client.created ",
            " web-bff ",
            " service_client ",
            " orders-api ",
            IdentityTestData.NowUtc);

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal("service_client.created", auditEvent.EventType);
        Assert.Equal("web-bff", auditEvent.ActorClientId);
        Assert.Equal("service_client", auditEvent.SubjectType);
        Assert.Equal("orders-api", auditEvent.SubjectId);
        Assert.Equal(IdentityTestData.NowUtc, auditEvent.OccurredAtUtc);
    }

    [Theory(DisplayName = "Criar evento de auditoria - deve rejeitar campos obrigatórios vazios")]
    [InlineData("", "web-bff", "service_client", "orders-api")]
    [InlineData("service_client.created", "", "service_client", "orders-api")]
    [InlineData("service_client.created", "web-bff", "", "orders-api")]
    [InlineData("service_client.created", "web-bff", "service_client", "")]
    public void Create_WithMissingRequiredField_RejectsInvalidState(
        string eventType,
        string actorClientId,
        string subjectType,
        string subjectId) =>
        Assert.Throws<DomainRuleViolationException>(() =>
            SecurityAuditEvent.Create(
                eventType,
                actorClientId,
                subjectType,
                subjectId,
                IdentityTestData.NowUtc));

    [Fact(DisplayName = "Criar evento de auditoria - deve rejeitar data fora de UTC")]
    public void Create_WithNonUtcDate_RejectsInvalidState()
    {
        var nonUtcDate = IdentityTestData.NowUtc.ToOffset(TimeSpan.FromHours(-3));

        Assert.Throws<DomainRuleViolationException>(() =>
            SecurityAuditEvent.Create(
                "service_client.created",
                "web-bff",
                "service_client",
                "orders-api",
                nonUtcDate));
    }
}
