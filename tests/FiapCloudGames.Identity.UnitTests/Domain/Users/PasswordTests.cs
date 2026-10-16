using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.UnitTests.Domain.Users;

public sealed class PasswordTests
{
    [Theory(DisplayName = "Criar senha - deve aceitar senha forte")]
    [InlineData("Password1!")]
    [InlineData("Outra-senha2")]
    [InlineData("Abcdef1!")]
    public void TryCreate_AcceptsStrongPassword(string value) =>
        Assert.True(Password.TryCreate(value, out _));

    [Theory(DisplayName = "Criar senha - deve rejeitar senha fraca")]
    [InlineData("short1!")]
    [InlineData("onlyletters!")]
    [InlineData("OnlyLetters1")]
    public void TryCreate_RejectsWeakPassword(string value) =>
        Assert.False(Password.TryCreate(value, out _));
}
