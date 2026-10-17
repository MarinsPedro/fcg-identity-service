using FiapCloudGames.Identity.Application.Validation;

namespace FiapCloudGames.Identity.UnitTests.Application.Validation;

public sealed class InputFieldNameTests
{
    [Theory(DisplayName = "Formatar nome de campo - deve converter propriedade para camelCase")]
    [InlineData("SubjectType", "subjectType")]
    [InlineData("IPAddress", "ipAddress")]
    [InlineData("Id", "id")]
    [InlineData("X", "x")]
    public void From_WithPropertyName_ShouldReturnPublicFieldName(
        string propertyName,
        string expected)
    {
        var result = InputFieldName.From(propertyName);

        Assert.Equal(expected, result);
    }
}
