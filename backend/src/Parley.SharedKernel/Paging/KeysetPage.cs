namespace Parley.SharedKernel.Paging;

/// <summary>
/// Builds a page from rows fetched with a limit of <c>limit + 1</c>. The extra row
/// tells whether another page exists without a count query.
/// </summary>
public static class KeysetPage
{
    public static CursorPage<TItem> From<TItem>(IReadOnlyList<TItem> fetched, int limit, Func<TItem, Guid> cursorOf)
    {
        ArgumentNullException.ThrowIfNull(fetched);
        ArgumentNullException.ThrowIfNull(cursorOf);

        if (fetched.Count <= limit)
        {
            return new CursorPage<TItem>(fetched, null);
        }

        var items = fetched.Take(limit).ToArray();
        return new CursorPage<TItem>(items, cursorOf(items[^1]));
    }
}
