using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.Sessions;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IsUsable_BeforeExpirationAndNotRevoked_ReturnsTrue()
    {
        var token = CreateToken();

        Assert.True(token.IsUsable(IdentityTestData.NowUtc.AddMinutes(30)));
    }

    [Fact]
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

    [Fact]
    public void Rotate_WhenAlreadyRevoked_RejectsSecondRotation()
    {
        var token = CreateToken();
        token.Revoke(IdentityTestData.NowUtc.AddMinutes(5));

        Assert.Throws<DomainRuleViolationException>(() =>
            token.Rotate(Guid.NewGuid(), IdentityTestData.NowUtc.AddMinutes(10)));
    }

    private static RefreshToken CreateToken() =>
        RefreshToken.Create(
            Guid.NewGuid(),
            "web-bff",
            "refresh-token-hash",
            IdentityTestData.NowUtc,
            IdentityTestData.NowUtc.AddHours(1));
}
