using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class AdminSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<AdminSeedOptions> optionsAccessor,
    TimeProvider clock,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        if (!IsRequested(options))
        {
            logger.LogInformation(
                "Seed do administrador não configurado; nenhuma alteração foi realizada.");
            return;
        }

        var email = ResolveEmail(options.Email);
        var password = ResolvePassword(options.Password);
        var name = ResolveName(options.Name);
        var existingUser = await context.Users.SingleOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.Role != UserRole.Administrator)
            {
                throw new InvalidOperationException(
                    $"O e-mail '{email.Value}' já pertence a um usuário que não é administrador.");
            }

            logger.LogInformation(
                "Administrador {AdminEmail} já existe; seed ignorado.",
                email.Value);
            return;
        }

        var administrator = User.Create(
            name,
            email,
            passwordHasher.Hash(password),
            clock.GetUtcNow(),
            UserRole.Administrator);

        await context.Users.AddAsync(administrator, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Administrador {AdminEmail} criado pelo seed.",
            email.Value);
    }

    private static bool IsRequested(AdminSeedOptions options) =>
        !string.IsNullOrWhiteSpace(options.Email) ||
        !string.IsNullOrWhiteSpace(options.Password);

    private static Email ResolveEmail(string? configuredEmail)
    {
        if (!Email.TryCreate(configuredEmail, out var email))
        {
            throw new InvalidOperationException(
                "Configure 'Admin:Email' com um e-mail válido.");
        }

        return email;
    }

    private static Password ResolvePassword(string? configuredPassword)
    {
        if (!Password.TryCreate(configuredPassword, out var password))
        {
            throw new InvalidOperationException(
                $"Configure 'Admin:Password' corretamente. {Password.InvalidMessage}");
        }

        return password;
    }

    private static string ResolveName(string? configuredName) =>
        string.IsNullOrWhiteSpace(configuredName)
            ? "Administrador"
            : configuredName.Trim();
}
