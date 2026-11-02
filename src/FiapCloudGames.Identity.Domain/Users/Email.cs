using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.Users;

public sealed record Email
{
    public const int MaximumLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string value)
    {
        if (!TryCreate(value, out var email))
        {
            throw new DomainRuleViolationException("O e-mail informado é inválido.");
        }

        return email;
    }

    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out Email? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength ||
            !MailAddress.TryCreate(normalized, out var address) ||
            !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        email = new Email(normalized);
        return true;
    }

    public override string ToString() => Value;
}
