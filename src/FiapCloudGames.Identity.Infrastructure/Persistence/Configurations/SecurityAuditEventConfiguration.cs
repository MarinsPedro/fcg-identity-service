using FiapCloudGames.Identity.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class SecurityAuditEventConfiguration
    : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("security_audit_events");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(auditEvent => auditEvent.EventType).HasColumnName("event_type")
            .HasMaxLength(SecurityAuditEvent.EventTypeMaximumLength).IsRequired();
        builder.Property(auditEvent => auditEvent.ActorClientId).HasColumnName("actor_client_id")
            .HasMaxLength(SecurityAuditEvent.ActorClientIdMaximumLength).IsRequired();
        builder.Property(auditEvent => auditEvent.SubjectType).HasColumnName("subject_type")
            .HasMaxLength(SecurityAuditEvent.SubjectTypeMaximumLength).IsRequired();
        builder.Property(auditEvent => auditEvent.SubjectId).HasColumnName("subject_id")
            .HasMaxLength(SecurityAuditEvent.SubjectIdMaximumLength).IsRequired();
        builder.Property(auditEvent => auditEvent.OccurredAtUtc).HasColumnName("occurred_at_utc")
            .IsRequired();
        builder.HasIndex(auditEvent => new
        {
            auditEvent.OccurredAtUtc,
            auditEvent.Id
        });
        builder.HasIndex(auditEvent => new
        {
            auditEvent.EventType,
            auditEvent.OccurredAtUtc,
            auditEvent.Id
        });
        builder.HasIndex(auditEvent => new
        {
            auditEvent.ActorClientId,
            auditEvent.OccurredAtUtc,
            auditEvent.Id
        });
        builder.HasIndex(auditEvent => new
        {
            auditEvent.SubjectType,
            auditEvent.SubjectId,
            auditEvent.OccurredAtUtc,
            auditEvent.Id
        });
    }
}
