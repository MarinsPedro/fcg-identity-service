using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;

namespace FiapCloudGames.Identity.Domain.Sessions;

public sealed class RefreshToken
{
    public const int TokenHashMaximumLength = 64;

    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string clientId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id != Guid.Empty
            ? id
            : throw new DomainRuleViolationException(
                "O identificador do refresh token é obrigatório.");
        UserId = userId != Guid.Empty
            ? userId
            : throw new DomainRuleViolationException(
                "O identificador do usuário é obrigatório.");
        ClientId = NormalizeRequired(
            clientId,
            ServiceClient.ClientIdMaximumLength,
            "client_id");
        TokenHash = NormalizeRequired(
            tokenHash,
            TokenHashMaximumLength,
            "hash do refresh token");
        CreatedAtUtc = ValidateUtc(createdAtUtc, "data de criação");
        ExpiresAtUtc = ValidateUtc(expiresAtUtc, "data de expiração");
        if (ExpiresAtUtc <= CreatedAtUtc)
        {
            throw new DomainRuleViolationException(
                "A expiração do refresh token deve ser posterior à criação.");
        }
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsUsable(DateTimeOffset now) =>
        RevokedAtUtc is null && ExpiresAtUtc > now;

    public static RefreshToken Create(
        Guid userId,
        string clientId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc) =>
        new(Guid.NewGuid(), userId, clientId, tokenHash, createdAtUtc, expiresAtUtc);

    public void Rotate(Guid replacementId, DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is not null)
        {
            throw new DomainRuleViolationException("O refresh token já foi revogado.");
        }

        ReplacedByTokenId = replacementId != Guid.Empty
            ? replacementId
            : throw new DomainRuleViolationException(
                "O identificador do refresh token substituto é obrigatório.");
        RevokedAtUtc = ValidateRevocationDate(revokedAtUtc);
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = ValidateRevocationDate(revokedAtUtc);
        }
    }

    private DateTimeOffset ValidateRevocationDate(DateTimeOffset value)
    {
        var revokedAtUtc = ValidateUtc(value, "data de revogação");
        return revokedAtUtc >= CreatedAtUtc
            ? revokedAtUtc
            : throw new DomainRuleViolationException(
                "A revogação do refresh token não pode ser anterior à criação.");
    }

    private static DateTimeOffset ValidateUtc(DateTimeOffset value, string field) =>
        value != default && value.Offset == TimeSpan.Zero
            ? value
            : throw new DomainRuleViolationException(
                $"A {field} deve estar em UTC.");

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string field) =>
        value?.Trim() is { Length: > 0 } normalized &&
        normalized.Length <= maximumLength
            ? normalized
            : throw new DomainRuleViolationException($"O {field} é inválido.");
}
