using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.Users;

public sealed class User
{
    public const int NameMinimumLength = 2;
    public const int NameMaximumLength = 120;
    public const int PasswordHashMaximumLength = 500;

    private User() { }

    private User(
        Guid id,
        string name,
        Email email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAtUtc)
    {
        Id = id != Guid.Empty
            ? id
            : throw new DomainRuleViolationException("O identificador do usuário é obrigatório.");
        ChangeDetails(name, email);
        PasswordHash = ValidatePasswordHash(passwordHash);
        Role = role is not UserRole.Undefined && Enum.IsDefined(role)
            ? role
            : throw new DomainRuleViolationException("O perfil do usuário é inválido.");
        IsActive = true;
        CreatedAtUtc = createdAtUtc != default && createdAtUtc.Offset == TimeSpan.Zero
            ? createdAtUtc
            : throw new DomainRuleViolationException("A data de criação deve estar em UTC.");
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static User Create(
        string name,
        Email email,
        string passwordHash,
        DateTimeOffset createdAtUtc,
        UserRole role = UserRole.User) =>
        new(Guid.NewGuid(), name, email, passwordHash, role, createdAtUtc);

    public void ChangeDetails(string name, Email email)
    {
        Name = name?.Trim() is
        {
            Length: >= NameMinimumLength and <= NameMaximumLength
        } normalized
            ? normalized
            : throw new DomainRuleViolationException(
                $"O nome deve ter entre {NameMinimumLength} e {NameMaximumLength} caracteres.");
        Email = email ?? throw new DomainRuleViolationException("O e-mail é obrigatório.");
    }

    public void Deactivate() => IsActive = false;

    private static string ValidatePasswordHash(string? passwordHash) =>
        passwordHash?.Trim() is
        {
            Length: > 0 and <= PasswordHashMaximumLength
        } normalized
            ? normalized
            : throw new DomainRuleViolationException("O hash da senha é inválido.");
}
