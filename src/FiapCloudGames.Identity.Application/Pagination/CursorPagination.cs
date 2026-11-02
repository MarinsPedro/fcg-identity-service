using FiapCloudGames.BuildingBlocks.Exceptions;

namespace FiapCloudGames.Identity.Application.Pagination;

/// <summary>
/// Centraliza limites e montagem de páginas baseadas em cursor.
/// </summary>
public static class CursorPagination
{
    /// <summary>
    /// Quantidade padrão de itens por página.
    /// </summary>
    public const int DefaultLimit = 50;

    /// <summary>
    /// Menor quantidade aceita por página.
    /// </summary>
    public const int MinimumLimit = 1;

    /// <summary>
    /// Maior quantidade aceita por página.
    /// </summary>
    public const int MaximumLimit = 200;

    /// <summary>
    /// Valida se o limite pertence ao intervalo suportado.
    /// </summary>
    /// <param name="limit">Limite solicitado.</param>
    public static void ValidateLimit(int limit)
    {
        if (limit is < MinimumLimit or > MaximumLimit)
        {
            throw AppException.Validation(
                $"O limite deve estar entre {MinimumLimit} e {MaximumLimit}.");
        }
    }

    /// <summary>
    /// Obtém a quantidade que o repository deve buscar para detectar outra página.
    /// </summary>
    /// <param name="limit">Limite validado da página.</param>
    /// <returns>Limite acrescido do item de verificação.</returns>
    public static int GetFetchLimit(int limit)
    {
        ValidateLimit(limit);
        return limit + 1;
    }

    /// <summary>
    /// Monta uma página usando o item excedente para detectar continuidade.
    /// </summary>
    /// <typeparam name="TSource">Tipo retornado pelo repository.</typeparam>
    /// <typeparam name="TItem">Tipo exposto pelo caso de uso.</typeparam>
    /// <typeparam name="TCursor">Tipo da posição usada para continuar a consulta.</typeparam>
    /// <param name="records">Registros ordenados, limitados a <c>limit + 1</c>.</param>
    /// <param name="limit">Quantidade máxima de itens expostos.</param>
    /// <param name="map">Conversão do registro para item do resultado.</param>
    /// <param name="cursorSelector">Extração do cursor a partir do último registro exposto.</param>
    /// <returns>Página de itens e cursor opcional da próxima página.</returns>
    public static CursorPageResult<TItem, TCursor> CreatePage<TSource, TItem, TCursor>(
        IReadOnlyList<TSource> records,
        int limit,
        Func<TSource, TItem> map,
        Func<TSource, TCursor> cursorSelector)
        where TCursor : struct
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(cursorSelector);
        ValidateLimit(limit);

        var hasNextPage = records.Count > limit;
        var pageRecords = records.Take(limit).ToArray();
        var nextCursor = hasNextPage
            ? cursorSelector(pageRecords[^1])
            : (TCursor?)null;

        return new(
            pageRecords.Select(map).ToArray(),
            nextCursor);
    }
}
