using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Enums;
using FiapCloudGames.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FiapCloudGames.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IIdentityStore
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceClient> ServiceClients => Set<ServiceClient>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public Task AddUserAsync(User user, CancellationToken cancellationToken) =>
        Users.AddAsync(user, cancellationToken).AsTask();

    public Task<User?> FindUserByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> UserEmailExistsAsync(
        Email email,
        Guid? exceptUserId,
        CancellationToken cancellationToken) =>
        Users.AnyAsync(
            user => user.Email == email &&
                (!exceptUserId.HasValue || user.Id != exceptUserId.Value),
            cancellationToken);

    public Task<ServiceClient?> FindClientAsync(
        string clientId,
        CancellationToken cancellationToken) =>
        ServiceClients.SingleOrDefaultAsync(client => client.ClientId == clientId, cancellationToken);

    public Task AddClientAsync(ServiceClient client, CancellationToken cancellationToken) =>
        ServiceClients.AddAsync(client, cancellationToken).AsTask();

    public Task<RefreshToken?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task AddRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken) =>
        RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();

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
            builder.Property(client => client.AllowedAudiences).HasColumnName("allowed_audiences").HasMaxLength(1000).IsRequired();
            builder.Property(client => client.AllowedScopes).HasColumnName("allowed_scopes").HasMaxLength(2000).IsRequired();
            builder.Property(client => client.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(client => client.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
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
