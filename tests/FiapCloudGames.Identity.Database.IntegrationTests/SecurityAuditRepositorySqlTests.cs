using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Database.IntegrationTests;

public sealed class SecurityAuditRepositorySqlTests
{
    [Fact(DisplayName = "Paginar auditoria - deve traduzir o cursor como comparação de tupla no PostgreSQL")]
    public void BuildQuery_WithCursor_ShouldUsePostgresRowComparison()
    {
        using var context = IdentityDbContextTestFactory.Create();
        var cursor = new SecurityAuditEventCursor(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero),
            Guid.Parse("a1e3277c-6fb5-4749-8b37-6a59de95fe5b"));
        var query = new SecurityAuditEventQuery(
            "service_client.created",
            "web-bff",
            "service_client",
            "orders-api",
            cursor.OccurredAtUtc.AddDays(-1),
            cursor.OccurredAtUtc,
            cursor);

        var sql = SecurityAuditRepository
            .BuildQuery(context, query)
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Take(51)
            .ToQueryString();

        Assert.Contains(
            "(s.occurred_at_utc, s.id) <",
            sql,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
    }
}
