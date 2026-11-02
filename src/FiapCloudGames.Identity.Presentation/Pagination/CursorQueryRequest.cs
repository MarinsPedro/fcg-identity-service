using System.ComponentModel;
using FiapCloudGames.Identity.Application.Pagination;
using FiapCloudGames.Identity.Presentation.Validation;

namespace FiapCloudGames.Identity.Presentation.Pagination;

/// <summary>
/// Define os parâmetros HTTP comuns das consultas paginadas por cursor.
/// </summary>
public abstract class CursorQueryRequest
{
    /// <summary>
    /// Quantidade máxima de itens da página. O padrão é 50.
    /// </summary>
    [ApiIntegerRange(
        CursorPagination.MinimumLimit,
        CursorPagination.MaximumLimit)]
    [DefaultValue(CursorPagination.DefaultLimit)]
    public int Limit { get; init; } = CursorPagination.DefaultLimit;

    /// <summary>
    /// Cursor opaco retornado em <c>nextCursor</c> pela página anterior.
    /// </summary>
    [ApiStringLength(CursorTokenCodec.MaximumTokenLength)]
    public string? After { get; init; }
}
