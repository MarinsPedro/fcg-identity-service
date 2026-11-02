using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.UnitTests.Domain.Users;

public sealed class EmailTests
{
    [Fact(DisplayName = "Criar e-mail - deve normalizar o valor")]
    public void Create_NormalizesEmail()
    {
        var email = Email.Create("  USER@Example.com ");
        Assert.Equal("user@example.com", email.Value);
    }

    [Theory(DisplayName = "Criar e-mail - deve rejeitar valor inválido")]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    public void TryCreate_RejectsInvalidEmail(string value) =>
        Assert.False(Email.TryCreate(value, out _));
}
