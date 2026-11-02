using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FiapCloudGames.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IIdentityUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceClient> ServiceClients => Set<ServiceClient>();
    public DbSet<ServiceClientAudience> ServiceClientAudiences => Set<ServiceClientAudience>();
    public DbSet<ServiceClientScope> ServiceClientScopes => Set<ServiceClientScope>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.AddInboxStateEntity(entity =>
            entity.ToTable("inbox_states"));
        modelBuilder.AddOutboxMessageEntity(entity =>
            entity.ToTable("outbox_messages"));
        modelBuilder.AddOutboxStateEntity(entity =>
            entity.ToTable("outbox_states"));
        ApplyLowerSnakeCaseToMassTransitEntities(modelBuilder);
    }

    private static void ApplyLowerSnakeCaseToMassTransitEntities(
        ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
                     .Where(entity => entity.ClrType.Namespace?.StartsWith(
                         "MassTransit",
                         StringComparison.Ordinal) == true))
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToLowerSnakeCase(property.Name));
            }
        }
    }

    private static string ToLowerSnakeCase(string value) =>
        string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0
                ? $"_{char.ToLowerInvariant(character)}"
                : char.ToLowerInvariant(character).ToString()));
}
