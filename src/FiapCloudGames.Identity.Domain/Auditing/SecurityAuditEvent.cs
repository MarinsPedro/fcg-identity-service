using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.Auditing;

public sealed class SecurityAuditEvent
{
    private SecurityAuditEvent() { }

    private SecurityAuditEvent(
        Guid id,
        string eventType,
        string actorClientId,
        string subjectType,
        string subjectId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id != Guid.Empty
            ? id
            : throw new DomainRuleViolationException(
                "O identificador do evento de auditoria é obrigatório.");
        EventType = NormalizeRequired(eventType, 100, "tipo do evento");
        ActorClientId = NormalizeRequired(actorClientId, 100, "client autor");
        SubjectType = NormalizeRequired(subjectType, 50, "tipo do objeto");
        SubjectId = NormalizeRequired(subjectId, 254, "objeto");
        OccurredAtUtc = occurredAtUtc != default && occurredAtUtc.Offset == TimeSpan.Zero
            ? occurredAtUtc
            : throw new DomainRuleViolationException(
                "A data do evento de auditoria deve estar em UTC.");
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string ActorClientId { get; private set; } = string.Empty;
    public string SubjectType { get; private set; } = string.Empty;
    public string SubjectId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static SecurityAuditEvent Create(
        string eventType,
        string actorClientId,
        string subjectType,
        string subjectId,
        DateTimeOffset occurredAtUtc) =>
        new(
            Guid.NewGuid(),
            eventType,
            actorClientId,
            subjectType,
            subjectId,
            occurredAtUtc);

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string field) =>
        value?.Trim() is { Length: > 0 } normalized &&
        normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
