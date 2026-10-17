using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ServiceClientScopeConfiguration
    : IEntityTypeConfiguration<ServiceClientScope>
{
    public void Configure(EntityTypeBuilder<ServiceClientScope> builder)
    {
        builder.ToTable("service_client_scopes");
        builder.HasKey(scope => new { scope.ClientId, scope.Scope });
        builder.Property(scope => scope.ClientId).HasColumnName("client_id")
            .HasMaxLength(ServiceClient.ClientIdMaximumLength);
        builder.Property(scope => scope.Scope).HasColumnName("scope")
            .HasMaxLength(ServiceClientScope.ScopeMaximumLength);
        builder.HasIndex(scope => scope.Scope);
        builder.HasOne<ServiceClient>()
            .WithMany(client => client.AllowedScopes)
            .HasForeignKey(scope => scope.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
