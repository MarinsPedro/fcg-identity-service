using FiapCloudGames.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var emailConverter = new ValueConverter<Email, string>(
            email => email.Value,
            value => Email.Create(value));

        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.Name).HasColumnName("name")
            .HasMaxLength(User.NameMaximumLength).IsRequired();
        builder.Property(user => user.Email).HasColumnName("email")
            .HasMaxLength(Email.MaximumLength)
            .HasConversion(emailConverter).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash")
            .HasMaxLength(User.PasswordHashMaximumLength).IsRequired();
        builder.Property(user => user.Role).HasColumnName("role").HasConversion<int>()
            .HasDefaultValue(UserRole.User).HasSentinel(UserRole.Undefined).IsRequired();
        builder.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}
