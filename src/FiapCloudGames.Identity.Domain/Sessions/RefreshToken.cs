using FiapCloudGames.Identity.Domain.Common;

namespace FiapCloudGames.Identity.Domain.Sessions;

public sealed class RefreshToken
{
    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string clientId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        ClientId = clientId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
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

        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenId = replacementId;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = revokedAtUtc;
        }
    }
}
