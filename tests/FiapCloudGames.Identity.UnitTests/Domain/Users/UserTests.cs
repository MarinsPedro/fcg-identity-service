using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.UnitTests.Support;

namespace FiapCloudGames.Identity.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact(DisplayName = "Criar usuário - deve criar usuário ativo com dados normalizados")]
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

    [Fact(DisplayName = "Criar usuário - deve rejeitar data de criação fora de UTC")]
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

    [Fact(DisplayName = "Criar usuário - deve rejeitar hash de senha acima do limite")]
    public void Create_WithOversizedPasswordHash_RejectsInvalidState()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            User.Create(
                "Aluno FIAP",
                Email.Create("aluno@fiap.com.br"),
                new string('a', User.PasswordHashMaximumLength + 1),
                IdentityTestData.NowUtc));
    }

    [Fact(DisplayName = "Desativar usuário - deve alterar o estado para inativo")]
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

    [Theory(DisplayName = "Alterar usuário - deve rejeitar nome fora dos limites")]
    [InlineData("")]
    [InlineData("A")]
    public void ChangeDetails_WithInvalidName_RejectsInvalidState(string name)
    {
        var user = IdentityTestData.CreateUser();

        Assert.Throws<DomainRuleViolationException>(() =>
            user.ChangeDetails(
                name,
                Email.Create("novo@fiap.com.br")));
    }
}
