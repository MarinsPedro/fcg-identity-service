using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Pagination;

namespace FiapCloudGames.Identity.UnitTests.Application.Pagination;

public sealed class CursorPaginationTests
{
    [Fact(DisplayName = "Validar paginação - deve aceitar limites mínimo e máximo")]
    public void ValidateLimit_WithSupportedBoundaries_ShouldNotThrow()
    {
        CursorPagination.ValidateLimit(CursorPagination.MinimumLimit);
        CursorPagination.ValidateLimit(CursorPagination.MaximumLimit);
    }

    [Theory(DisplayName = "Validar paginação - deve rejeitar limite fora do intervalo")]
    [InlineData(0)]
    [InlineData(201)]
    public void ValidateLimit_WithUnsupportedLimit_ShouldReturnValidation(int limit)
    {
        var exception = Assert.Throws<AppException>(() =>
            CursorPagination.ValidateLimit(limit));

        Assert.Equal(AppErrorCategory.Validation, exception.Category);
    }

    [Fact(DisplayName = "Obter limite de busca - deve reservar um item para detectar próxima página")]
    public void GetFetchLimit_WithPageLimit_ShouldAddOne()
    {
        var fetchLimit = CursorPagination.GetFetchLimit(50);

        Assert.Equal(51, fetchLimit);
    }

    [Fact(DisplayName = "Criar página - deve remover item excedente e gerar cursor do último item exposto")]
    public void CreatePage_WithExtraRecord_ShouldReturnNextCursor()
    {
        TestRecord[] records = [new(1), new(2), new(3)];

        var page = CursorPagination.CreatePage(
            records,
            2,
            record => record.Id.ToString(),
            record => record.Id);

        Assert.Equal(["1", "2"], page.Items);
        Assert.Equal(2, page.NextCursor);
    }

    [Fact(DisplayName = "Criar página - deve omitir cursor quando não houver item excedente")]
    public void CreatePage_WithoutExtraRecord_ShouldReturnNoCursor()
    {
        TestRecord[] records = [new(1), new(2)];

        var page = CursorPagination.CreatePage(
            records,
            2,
            record => record.Id.ToString(),
            record => record.Id);

        Assert.Equal(["1", "2"], page.Items);
        Assert.Null(page.NextCursor);
    }

    private sealed record TestRecord(int Id);
}
