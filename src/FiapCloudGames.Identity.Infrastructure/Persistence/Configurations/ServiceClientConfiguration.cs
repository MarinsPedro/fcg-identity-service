using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ServiceClientConfiguration : IEntityTypeConfiguration<ServiceClient>
{
    public void Configure(EntityTypeBuilder<ServiceClient> builder)
    {
        builder.ToTable("service_clients");
        builder.HasKey(client => client.ClientId);
        builder.Property(client => client.ClientId).HasColumnName("client_id").HasMaxLength(100);
        builder.Property(client => client.DisplayName).HasColumnName("display_name")
            .HasMaxLength(200).IsRequired();
        builder.Property(client => client.SecretHash).HasColumnName("secret_hash")
            .HasMaxLength(500).IsRequired();
        builder.Property(client => client.CanIssueUserTokens)
            .HasColumnName("can_issue_user_tokens").IsRequired();
        builder.Property(client => client.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(client => client.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Navigation(client => client.AllowedAudiences)
            .HasField("allowedAudiences")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(client => client.AllowedScopes)
            .HasField("allowedScopes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
