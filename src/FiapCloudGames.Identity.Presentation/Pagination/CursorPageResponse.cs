namespace FiapCloudGames.Identity.Presentation.Pagination;

/// <summary>
/// Representa uma página HTTP obtida por cursor.
/// </summary>
/// <typeparam name="TItem">Tipo dos itens retornados.</typeparam>
/// <param name="Items">Itens da página atual.</param>
/// <param name="NextCursor">Cursor opaco da próxima página ou <see langword="null" />.</param>
public sealed record CursorPageResponse<TItem>(
    IReadOnlyList<TItem> Items,
    string? NextCursor);
