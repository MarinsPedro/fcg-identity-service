using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Enums;
using FiapCloudGames.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FiapCloudGames.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IIdentityUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceClient> ServiceClients => Set<ServiceClient>();
    public DbSet<ServiceClientAudience> ServiceClientAudiences => Set<ServiceClientAudience>();
    public DbSet<ServiceClientScope> ServiceClientScopes => Set<ServiceClientScope>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var emailConverter = new ValueConverter<Email, string>(
            email => email.Value,
            value => Email.Create(value));

        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(user => user.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(254)
                .HasConversion(emailConverter).IsRequired();
            builder.HasIndex(user => user.Email).IsUnique();
            builder.Property(user => user.PasswordHash).HasColumnName("password_hash")
                .HasMaxLength(500).IsRequired();
            builder.Property(user => user.Role).HasColumnName("role").HasConversion<int>()
                .HasDefaultValue(UserRole.User).HasSentinel(UserRole.Undefined).IsRequired();
            builder.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        });

        modelBuilder.Entity<ServiceClient>(builder =>
        {
            builder.ToTable("service_clients");
            builder.HasKey(client => client.ClientId);
            builder.Property(client => client.ClientId).HasColumnName("client_id").HasMaxLength(100);
            builder.Property(client => client.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            builder.Property(client => client.SecretHash).HasColumnName("secret_hash").HasMaxLength(500).IsRequired();
            builder.Property(client => client.CanIssueUserTokens).HasColumnName("can_issue_user_tokens").IsRequired();
            builder.Property(client => client.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(client => client.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            builder.Navigation(client => client.AllowedAudiences)
                .HasField("allowedAudiences")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(client => client.AllowedScopes)
                .HasField("allowedScopes")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ServiceClientAudience>(builder =>
        {
            builder.ToTable("service_client_audiences");
            builder.HasKey(audience => new { audience.ClientId, audience.Audience });
            builder.Property(audience => audience.ClientId).HasColumnName("client_id").HasMaxLength(100);
            builder.Property(audience => audience.Audience).HasColumnName("audience").HasMaxLength(200);
            builder.HasIndex(audience => audience.Audience);
            builder.HasOne<ServiceClient>()
                .WithMany(client => client.AllowedAudiences)
                .HasForeignKey(audience => audience.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceClientScope>(builder =>
        {
            builder.ToTable("service_client_scopes");
            builder.HasKey(scope => new { scope.ClientId, scope.Scope });
            builder.Property(scope => scope.ClientId).HasColumnName("client_id").HasMaxLength(100);
            builder.Property(scope => scope.Scope).HasColumnName("scope").HasMaxLength(200);
            builder.HasIndex(scope => scope.Scope);
            builder.HasOne<ServiceClient>()
                .WithMany(client => client.AllowedScopes)
                .HasForeignKey(scope => scope.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(token => token.ClientId).HasColumnName("client_id").HasMaxLength(100).IsRequired();
            builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
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
        });
    }
}
