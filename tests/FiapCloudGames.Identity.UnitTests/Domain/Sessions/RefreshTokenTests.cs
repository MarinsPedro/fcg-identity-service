using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.Sessions;

public sealed class RefreshTokenTests
{
    [Fact(DisplayName = "Validar refresh token - deve aceitar token vigente e não revogado")]
    public void IsUsable_BeforeExpirationAndNotRevoked_ReturnsTrue()
    {
        var token = CreateToken();

        Assert.True(token.IsUsable(IdentityTestData.NowUtc.AddMinutes(30)));
    }

    [Fact(DisplayName = "Rotacionar refresh token - deve revogar o token atual e vincular o substituto")]
    public void Rotate_WhenValid_RevokesCurrentTokenAndLinksReplacement()
    {
        var token = CreateToken();
        var replacementId = Guid.NewGuid();
        var revokedAtUtc = IdentityTestData.NowUtc.AddMinutes(5);

        token.Rotate(replacementId, revokedAtUtc);

        Assert.Equal(revokedAtUtc, token.RevokedAtUtc);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
        Assert.False(token.IsUsable(revokedAtUtc));
    }

    [Fact(DisplayName = "Rotacionar refresh token - deve rejeitar uma segunda rotação")]
    public void Rotate_WhenAlreadyRevoked_RejectsSecondRotation()
    {
        var token = CreateToken();
        token.Revoke(IdentityTestData.NowUtc.AddMinutes(5));

        Assert.Throws<DomainRuleViolationException>(() =>
            token.Rotate(Guid.NewGuid(), IdentityTestData.NowUtc.AddMinutes(10)));
    }

    [Fact(DisplayName = "Validar refresh token - deve rejeitar token no instante exato da expiração")]
    public void IsUsable_AtExpiration_ReturnsFalse()
    {
        var token = CreateToken();

        Assert.False(token.IsUsable(IdentityTestData.NowUtc.AddHours(1)));
    }

    [Fact(DisplayName = "Revogar refresh token - deve ser idempotente e preservar a primeira data")]
    public void Revoke_WhenAlreadyRevoked_PreservesFirstRevocationDate()
    {
        var token = CreateToken();
        var firstRevocation = IdentityTestData.NowUtc.AddMinutes(5);
        token.Revoke(firstRevocation);

        token.Revoke(IdentityTestData.NowUtc.AddMinutes(10));

        Assert.Equal(firstRevocation, token.RevokedAtUtc);
    }

    [Fact(DisplayName = "Criar refresh token - deve rejeitar identificadores e hash obrigatórios ausentes")]
    public void Create_WithMissingRequiredData_RejectsInvalidState()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            RefreshToken.Create(
                Guid.Empty,
                "web-bff",
                "refresh-token-hash",
                IdentityTestData.NowUtc,
                IdentityTestData.NowUtc.AddHours(1)));
        Assert.Throws<DomainRuleViolationException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                "",
                "refresh-token-hash",
                IdentityTestData.NowUtc,
                IdentityTestData.NowUtc.AddHours(1)));
        Assert.Throws<DomainRuleViolationException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                "web-bff",
                "",
                IdentityTestData.NowUtc,
                IdentityTestData.NowUtc.AddHours(1)));
    }

    [Fact(DisplayName = "Criar refresh token - deve exigir expiração posterior à criação em UTC")]
    public void Create_WithInvalidDates_RejectsInvalidState()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                "web-bff",
                "refresh-token-hash",
                IdentityTestData.NowUtc,
                IdentityTestData.NowUtc));
        Assert.Throws<DomainRuleViolationException>(() =>
            RefreshToken.Create(
                Guid.NewGuid(),
                "web-bff",
                "refresh-token-hash",
                IdentityTestData.NowUtc.ToOffset(TimeSpan.FromHours(-3)),
                IdentityTestData.NowUtc.AddHours(1)));
    }

    private static RefreshToken CreateToken() =>
        RefreshToken.Create(
            Guid.NewGuid(),
            "web-bff",
            "refresh-token-hash",
            IdentityTestData.NowUtc,
            IdentityTestData.NowUtc.AddHours(1));
}
