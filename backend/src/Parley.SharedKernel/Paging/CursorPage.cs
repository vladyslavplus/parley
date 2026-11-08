namespace Parley.SharedKernel.Paging;

/// <summary>
/// One page of a keyset-paged list. <see cref="NextCursor"/> is the identifier of the
/// last item when more rows exist, and null on the last page.
/// </summary>
/// <param name="Items">Rows of this page, in list order.</param>
/// <param name="NextCursor">Cursor for the following page, or null when this is the last page.</param>
public sealed record CursorPage<TItem>(IReadOnlyList<TItem> Items, Guid? NextCursor);
