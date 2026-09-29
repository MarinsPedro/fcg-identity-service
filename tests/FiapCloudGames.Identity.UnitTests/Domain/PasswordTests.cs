using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.UnitTests.Domain;

public sealed class PasswordTests
{
    [Theory]
    [InlineData("Password1!")]
    [InlineData("Outra-senha2")]
    public void TryCreate_AcceptsStrongPassword(string value) =>
        Assert.True(Password.TryCreate(value, out _));

    [Theory]
    [InlineData("short1!")]
    [InlineData("onlyletters!")]
    [InlineData("OnlyLetters1")]
    public void TryCreate_RejectsWeakPassword(string value) =>
        Assert.False(Password.TryCreate(value, out _));
}
