namespace FiapCloudGames.Identity.Application.Pagination;

/// <summary>
/// Representa uma página obtida por cursor, sem executar contagem total.
/// </summary>
/// <typeparam name="TItem">Tipo dos itens retornados.</typeparam>
/// <typeparam name="TCursor">Tipo da posição usada para continuar a consulta.</typeparam>
/// <param name="Items">Itens da página atual.</param>
/// <param name="NextCursor">Posição da próxima página ou <see langword="null" />.</param>
public sealed record CursorPageResult<TItem, TCursor>(
    IReadOnlyList<TItem> Items,
    TCursor? NextCursor)
    where TCursor : struct;
