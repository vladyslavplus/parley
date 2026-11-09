namespace Parley.SharedKernel.Paging;

/// <summary>
/// One page of a keyset list whose sort key is not a bare identifier. <see cref="NextCursor"/>
/// encodes the sort value and identifier of the last item, and is null on the last page.
/// </summary>
/// <param name="Items">Rows of this page, in list order.</param>
/// <param name="NextCursor">Opaque cursor for the following page, or null when this is the last page.</param>
public sealed record KeysetCursorPage<TItem>(IReadOnlyList<TItem> Items, string? NextCursor)
{
    /// <summary>
    /// Builds a page from rows fetched with a limit of <c>limit + 1</c>. The extra row tells
    /// whether another page exists without a count query.
    /// </summary>
    public static KeysetCursorPage<TItem> From(IReadOnlyList<TItem> fetched, int limit, Func<TItem, KeysetCursor> cursorOf)
    {
        ArgumentNullException.ThrowIfNull(fetched);
        ArgumentNullException.ThrowIfNull(cursorOf);

        if (fetched.Count <= limit)
        {
            return new KeysetCursorPage<TItem>(fetched, null);
        }

        var items = fetched.Take(limit).ToArray();
        return new KeysetCursorPage<TItem>(items, cursorOf(items[^1]).Encode());
    }
}
