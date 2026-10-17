using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ServiceClientAudienceConfiguration
    : IEntityTypeConfiguration<ServiceClientAudience>
{
    public void Configure(EntityTypeBuilder<ServiceClientAudience> builder)
    {
        builder.ToTable("service_client_audiences");
        builder.HasKey(audience => new { audience.ClientId, audience.Audience });
        builder.Property(audience => audience.ClientId).HasColumnName("client_id")
            .HasMaxLength(ServiceClient.ClientIdMaximumLength);
        builder.Property(audience => audience.Audience).HasColumnName("audience")
            .HasMaxLength(ServiceClientAudience.AudienceMaximumLength);
        builder.HasIndex(audience => audience.Audience);
        builder.HasOne<ServiceClient>()
            .WithMany(client => client.AllowedAudiences)
            .HasForeignKey(audience => audience.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
