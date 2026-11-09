using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Parley.SharedKernel.Paging;

/// <summary>
/// Reads one keyset page of any list. Rows are ordered on the source (columns or correlated subqueries), the
/// page is taken, and only then projected, so the database orders by the sort key and the cursor key is read
/// from the projected row with the same value.
/// </summary>
public static class KeysetPages
{
    /// <summary>
    /// Reads one page. Fetches one row more than <paramref name="limit"/> to learn whether another page exists.
    /// Returns null when the cursor was issued for another sort field or its key does not parse.
    /// </summary>
    public static async Task<KeysetCursorPage<TItem>?> ReadAsync<TSource, TItem, TKey>(
        IQueryable<TSource> source,
        KeysetCursor? cursor,
        string sortField,
        Expression<Func<TSource, TKey>> key,
        Expression<Func<TSource, Guid>> id,
        Expression<Func<TSource, TItem>> project,
        Func<TItem, TKey> keyOf,
        Func<TItem, Guid> idOf,
        Func<string, TKey?> parse,
        Func<TKey, string> format,
        bool descending,
        int limit,
        CancellationToken cancellationToken)
    {
        KeysetPosition<TKey>? position = null;
        if (cursor is not null)
        {
            position = KeysetOrdering.ReadPosition(cursor, sortField, parse);
            if (position is null)
            {
                return null;
            }
        }

        var fetched = await KeysetOrdering.Apply(source, key, id, position, descending)
            .Take(limit + 1)
            .Select(project)
            .ToListAsync(cancellationToken);
        return KeysetCursorPage<TItem>.From(fetched, limit, item => KeysetOrdering.CursorFor(sortField, format(keyOf(item)), idOf(item)));
    }
}
