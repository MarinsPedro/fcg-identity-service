using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.UnitTests.Domain.Users;

public sealed class EmailTests
{
    [Fact]
    public void Create_NormalizesEmail()
    {
        var email = Email.Create("  USER@Example.com ");
        Assert.Equal("user@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    public void TryCreate_RejectsInvalidEmail(string value) =>
        Assert.False(Email.TryCreate(value, out _));
}
