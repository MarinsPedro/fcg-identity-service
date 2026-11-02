using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.ClientId).HasColumnName("client_id")
            .HasMaxLength(ServiceClient.ClientIdMaximumLength).IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash")
            .HasMaxLength(RefreshToken.TokenHashMaximumLength).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.Property(token => token.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc")
            .IsConcurrencyToken();
        builder.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.HasIndex(token => new { token.UserId, token.ClientId });
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ServiceClient>().WithMany().HasForeignKey(token => token.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
