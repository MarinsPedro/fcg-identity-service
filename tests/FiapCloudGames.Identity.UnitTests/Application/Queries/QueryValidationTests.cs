using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Queries;

namespace FiapCloudGames.Identity.UnitTests.Application.Queries;

public sealed class QueryValidationTests
{
    [Theory(DisplayName = "Normalizar filtro opcional - deve converter valor ausente em nulo")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOptional_WithMissingValue_ShouldReturnNull(string? value)
    {
        var result = QueryValidation.NormalizeOptional(value, 10, "filter");

        Assert.Null(result);
    }

    [Fact(DisplayName = "Normalizar filtro opcional - deve remover espaços externos")]
    public void NormalizeOptional_WithValue_ShouldTrimValue()
    {
        var result = QueryValidation.NormalizeOptional(" value ", 10, "filter");

        Assert.Equal("value", result);
    }

    [Fact(DisplayName = "Normalizar filtro opcional - deve rejeitar tamanho acima do permitido")]
    public void NormalizeOptional_WithOversizedValue_ShouldReturnValidation()
    {
        var exception = Assert.Throws<AppException>(() =>
            QueryValidation.NormalizeOptional("123456", 5, "filter"));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
    }

    [Fact(DisplayName = "Validar data de consulta - deve aceitar valor UTC")]
    public void EnsureUtc_WithUtcValue_ShouldNotThrow()
    {
        var value = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        QueryValidation.EnsureUtc(value, "occurredAtUtc");
    }

    [Fact(DisplayName = "Validar data de consulta - deve rejeitar offset diferente de UTC")]
    public void EnsureUtc_WithNonUtcValue_ShouldReturnValidation()
    {
        var value = new DateTimeOffset(
            2026,
            10,
            17,
            9,
            0,
            0,
            TimeSpan.FromHours(-3));

        var exception = Assert.Throws<AppException>(() =>
            QueryValidation.EnsureUtc(value, "occurredAtUtc"));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
    }

    [Fact(DisplayName = "Validar período de consulta - deve aceitar intervalo crescente")]
    public void EnsureValidPeriod_WithOrderedDates_ShouldNotThrow()
    {
        var fromUtc = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var toUtc = fromUtc.AddDays(1);

        QueryValidation.EnsureValidPeriod(fromUtc, toUtc, "fromUtc", "toUtc");
    }

    [Fact(DisplayName = "Validar período de consulta - deve rejeitar intervalo invertido")]
    public void EnsureValidPeriod_WithInvertedDates_ShouldReturnValidation()
    {
        var toUtc = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var fromUtc = toUtc.AddDays(1);

        var exception = Assert.Throws<AppException>(() =>
            QueryValidation.EnsureValidPeriod(fromUtc, toUtc, "fromUtc", "toUtc"));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
    }
}
