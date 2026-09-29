using System.Diagnostics.CodeAnalysis;

namespace FiapCloudGames.Identity.Domain.ValueObjects;

public sealed record Password
{
    public const int MinimumLength = 8;
    public const string InvalidMessage =
        "A senha deve ter pelo menos 8 caracteres e conter letras, números e caracteres especiais.";

    private Password(string value) => Value = value;

    public string Value { get; }

    public static Password Create(string value)
    {
        if (!TryCreate(value, out var password))
        {
            throw new DomainRuleViolationException(InvalidMessage);
        }

        return password;
    }

    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out Password? password)
    {
        password = null;
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length < MinimumLength ||
            !value.Any(char.IsLetter) ||
            !value.Any(char.IsDigit) ||
            !value.Any(character => char.IsPunctuation(character) || char.IsSymbol(character)))
        {
            return false;
        }

        password = new Password(value);
        return true;
    }

    public override string ToString() => "[REDACTED]";
}
