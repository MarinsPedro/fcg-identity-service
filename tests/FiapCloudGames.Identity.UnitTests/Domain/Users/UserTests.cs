using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveUserWithNormalizedDetails()
    {
        var user = User.Create(
            "  Aluno FIAP  ",
            Email.Create("aluno@fiap.com.br"),
            "password-hash",
            IdentityTestData.NowUtc);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Aluno FIAP", user.Name);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(IdentityTestData.NowUtc, user.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithNonUtcCreationDate_RejectsInvalidState()
    {
        var nonUtcDate = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.FromHours(-3));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            User.Create(
                "Aluno FIAP",
                Email.Create("aluno@fiap.com.br"),
                "password-hash",
                nonUtcDate));

        Assert.Equal("A data de criação deve estar em UTC.", exception.Message);
    }

    [Fact]
    public void Deactivate_WhenActive_ChangesStateToInactive()
    {
        var user = User.Create(
            "Aluno FIAP",
            Email.Create("aluno@fiap.com.br"),
            "password-hash",
            IdentityTestData.NowUtc);

        user.Deactivate();

        Assert.False(user.IsActive);
    }
}
